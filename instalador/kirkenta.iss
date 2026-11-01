; ============================================================
; Instalador de Kirkenta ERP (Inno Setup 6)  -  se compila con tools\crear-instalador.ps1
; Instala: el sistema, MySQL propio (puerto 3307, solo local), el arranque automatico y los accesos directos.
; ============================================================
#ifndef Version
  #define Version "1.0.0"
#endif

[Setup]
AppId={{2F94CB08-48B3-488E-8EFC-5D5DF1B15D85}
AppName=Kirkenta ERP
AppVersion={#Version}
AppPublisher=Kirkenta
DefaultDirName=C:\Kirkenta
DefaultGroupName=Kirkenta
DisableProgramGroupPage=yes
OutputDir=salida
OutputBaseFilename=Kirkenta-Setup-{#Version}
Compression=lzma2
SolidCompression=yes
PrivilegesRequired=admin
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
WizardStyle=modern
UninstallDisplayIcon={app}\sistema\kirkenta.ico
SetupIconFile=recursos\kirkenta.ico
WizardImageFile=recursos\wizard-grande.bmp
WizardSmallImageFile=recursos\wizard-pequena.bmp
; Los datos del cliente (base de datos, App_Data) NO se borran al desinstalar ni al actualizar.

[Languages]
Name: "spanish"; MessagesFile: "compiler:Languages\Spanish.isl"

[Files]
; El sistema (sin configuracion ni datos: eso lo crea la instalacion)
Source: "..\publicar\Kirkenta-win-x64\*"; DestDir: "{app}\sistema"; Excludes: "appsettings.Production.json,App_Data\*,logs\*,respaldos\*,LEEME.txt,Iniciar-Kirkenta.cmd"; Flags: recursesubdirs ignoreversion
; Ícono de Kirkenta (lista de programas instalados y accesos directos)
Source: "recursos\kirkenta.ico"; DestDir: "{app}\sistema"; Flags: ignoreversion
; MySQL portable (preparado por crear-instalador.ps1)
; En una actualizacion NO se reemplaza (MySQL esta en uso y sus archivos quedan bloqueados; ademas asi nunca se toca la base de datos)
Source: "recursos\mysql\*"; DestDir: "{app}\mysql"; Flags: recursesubdirs ignoreversion; Check: MySqlNoInstalado
; Scripts de instalacion y mantenimiento
Source: "scripts\*"; DestDir: "{app}\mantenimiento"; Flags: ignoreversion
#if FileExists("recursos\vc_redist.x64.exe")
Source: "recursos\vc_redist.x64.exe"; DestDir: "{tmp}"; Flags: dontcopy
#endif

[Icons]
Name: "{autodesktop}\Kirkenta ERP"; Filename: "{sys}\cmd.exe"; Parameters: "/c start """" http://localhost:5114"; IconFilename: "{app}\sistema\kirkenta.ico"; Flags: runminimized
Name: "{group}\Kirkenta ERP"; Filename: "{sys}\cmd.exe"; Parameters: "/c start """" http://localhost:5114"; IconFilename: "{app}\sistema\kirkenta.ico"; Flags: runminimized
Name: "{group}\Desinstalar Kirkenta"; Filename: "{uninstallexe}"

[Run]
Filename: "http://localhost:5114"; Description: "Abrir Kirkenta ahora"; Flags: shellexec postinstall skipifsilent nowait

[UninstallRun]
Filename: "powershell.exe"; Parameters: "-NoProfile -ExecutionPolicy Bypass -File ""{app}\mantenimiento\desinstalar.ps1"" -Raiz ""{app}"""; Flags: runhidden waituntilterminated; RunOnceId: "QuitarServicios"

[Code]
function MySqlNoInstalado: Boolean;
begin
  Result := not FileExists(ExpandConstant('{app}\mysql\bin\mysqld.exe'));
end;

{ true si el equipo ya tiene el Visual C++ Redistributable 2022 (o mas nuevo): evita reinstalarlo en cada actualizacion }
function VcRedistInstalado: Boolean;
var
  instalado, bld: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Installed', instalado)
        and (instalado = 1)
        and RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64', 'Bld', bld)
        and (bld >= 35211);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var
  rc: Integer;
begin
  { Si es una actualizacion, detiene el sistema para poder reemplazar los archivos }
  Exec(ExpandConstant('{sys}\schtasks.exe'), '/End /TN "Kirkenta-Servicio"', '', SW_HIDE, ewWaitUntilTerminated, rc);
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/F /IM Kirkenta.exe', '', SW_HIDE, ewWaitUntilTerminated, rc);
  Sleep(3000); { da tiempo a que Windows libere los archivos }
  Result := '';
end;

procedure CurStepChanged(CurStep: TSetupStep);
var
  rc: Integer;
  ok: Boolean;
  txt: AnsiString;
  ruta: String;
begin
  if CurStep = ssPostInstall then
  begin
#if FileExists("recursos\vc_redist.x64.exe")
    if not VcRedistInstalado then
    begin
      WizardForm.StatusLabel.Caption := 'Instalando componentes de Windows...';
      ExtractTemporaryFile('vc_redist.x64.exe');
      Exec(ExpandConstant('{tmp}\vc_redist.x64.exe'), '/install /quiet /norestart', '', SW_HIDE, ewWaitUntilTerminated, rc);
    end;
#endif
    WizardForm.StatusLabel.Caption := 'Instalando la base de datos y el servicio (puede tardar un par de minutos)...';
    ok := Exec('powershell.exe',
      ExpandConstant('-NoProfile -ExecutionPolicy Bypass -File "{app}\mantenimiento\instalar.ps1" -Raiz "{app}"'),
      '', SW_HIDE, ewWaitUntilTerminated, rc);
    if (not ok) or (rc <> 0) then
      MsgBox('La instalacion termino con un problema (codigo ' + IntToStr(rc) + ').' + #13#10 +
             'Envia a soporte el archivo: ' + ExpandConstant('{app}\logs-instalacion\instalar.log'), mbError, MB_OK)
    else
    begin
      ruta := ExpandConstant('{app}\sistema\App_Data\primer-acceso.txt');
      if LoadStringFromFile(ruta, txt) then
        MsgBox('ANOTA estos datos para entrar la primera vez (cambia la clave dentro del sistema):' + #13#10#13#10 +
               String(txt) + #13#10 + 'Tambien quedan en: ' + ruta, mbInformation, MB_OK);

      { Clave de los respaldos cifrados: se muestra una sola vez y luego se borra el aviso }
      ruta := ExpandConstant('{app}\datos\aviso-claves.txt');
      if LoadStringFromFile(ruta, txt) then
      begin
        MsgBox('GUARDA ESTA INFORMACION (se muestra una sola vez):' + #13#10#13#10 + String(txt), mbInformation, MB_OK);
        DeleteFile(ruta);
      end;
    end;
  end;
end;
