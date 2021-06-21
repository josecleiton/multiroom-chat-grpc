dotnet-format uno-grpc.sln

/resharper/inspectcode.sh uno-grpc.sln --format=Text --output=/tmp/issues.txt --verbosity=WARN --swea && cat /tmp/issues.txt | grep -e '^      ' | grep -v 'is never used' | grep -v 'is never instantiated' | grep -v 'can be made private' | grep -v 'can be made readonly' | grep -v 'can be made get-only' | grep -v 'can be made init-only' | grep -v 'Redundant type specification' | grep -v 'Content of collection.*is never updated' | grep -v 'Migrations' | grep -v '.proto:' && rm /tmp/issues.txt || true
