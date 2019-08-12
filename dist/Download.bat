rem Run this to deploy andy web files and templates
rem Place this script in the root folder the portal configuration, 
rem along with the PortalDeployer.exe and its dependencies

PortalDeployer.exe download --directory %~dp0
