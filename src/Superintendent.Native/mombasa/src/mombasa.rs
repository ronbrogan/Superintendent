use tonic::{transport::Server};
use std::ffi::c_void;

mod server_lib;
use server_lib::Bridge;

mod mombasa_bridge { tonic::include_proto!("mombasa"); }
use mombasa_bridge::mombasa_bridge_server::MombasaBridgeServer;

#[no_mangle]
#[allow(non_snake_case)]
#[allow(unused_variables)]
pub extern "stdcall" fn DllMain(module: u32, reason: u32, reserved: *const c_void) {
    match reason {
        1 => initialize(),
        _ => ()
    }
}

#[no_mangle]
pub extern "C" fn initialize() {
    std::thread::spawn(|| {
        grpc_startup();
    });
}

#[no_mangle]
pub extern "C" fn grpc_startup() {
    
    println!("grpc_startup running");

    let runtime = tokio::runtime::Runtime::new().unwrap();

    runtime.block_on(async {
        let addr = "127.0.0.1:50051".parse().unwrap();
        let greeter = Bridge::default();

        println!("starting grpc server");

        Server::builder()
            .add_service(MombasaBridgeServer::new(greeter))
            .serve(addr)
            .await
            .unwrap();
    });

}