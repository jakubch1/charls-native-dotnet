msbuild -t:clean -p:Configuration=Release
msbuild -t:pack -p:Configuration=Release -p:ContinuousIntegrationBuild=true
