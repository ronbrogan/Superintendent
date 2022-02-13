protoc -I=. --cpp_out=. mombasa.proto 
protoc -I=. --grpc_out=. --plugin=protoc-gen-grpc="C:\src\vcpkg\packages\grpc_x64-windows\tools\grpc\grpc_cpp_plugin.exe" mombasa.proto 