nuget restore
msbuild /p:configuration=release,publicrelease=true /nologo /m /nr:false /p:BuildInParallel=true /v:minimal /fl /flp:verbosity=detailed
