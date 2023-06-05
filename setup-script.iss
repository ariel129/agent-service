[Setup]
AppName=Agent Service
AppVersion=1.0
DefaultDirName={code:GetProgramFilesDir}\AgentService
OutputBaseFilename=AgentServiceInstaller
OutputDir=userdocs:Inno Setup Examples Output
PrivilegesRequired=admin
ArchitecturesAllowed=x64

[Code]
function GetProgramFilesDir(Param: String): String;
begin
  if IsWin64 then
    Result := ExpandConstant('{commonpf64}')
  else
    Result := ExpandConstant('{commonpf32}');
end;

[Files]
Source: "bin\Release\net6.0\publish\win-x64\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Run]
Filename: "{app}\AgentService.exe"; Description: "Launch My Service"; Flags: postinstall runascurrentuser skipifsilent
