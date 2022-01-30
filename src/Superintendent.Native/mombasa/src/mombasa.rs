use once_cell::sync::OnceCell;

use directories::UserDirs;
use flexi_logger::{Logger,LoggerHandle,FileSpec,FlexiLoggerError,detailed_format};

use tonic::{transport::Server};
use std::ffi::c_void;
use winapi::um::winnt::{DLL_PROCESS_ATTACH, DLL_PROCESS_DETACH};
use triggered::{Trigger};

mod server_lib;
use server_lib::Bridge;

mod mombasa_bridge { tonic::include_proto!("mombasa"); }
use mombasa_bridge::mombasa_bridge_server::MombasaBridgeServer;

//lazy_static! {
//    static ref SHUTDOWN : Option<Trigger> = None;
//}

static SHUTDOWN: OnceCell<Trigger> = OnceCell::new();

#[no_mangle]
#[allow(non_snake_case)]
#[allow(unused_variables)]
pub extern "stdcall" fn DllMain(module: u32, reason: u32, reserved: *const c_void) {
    match reason {
        DLL_PROCESS_ATTACH => initialize(),
        DLL_PROCESS_DETACH => teardown(),
        _ => ()
    }
}

#[no_mangle]
pub extern "C" fn initialize() {
    let _ = setup_logging();

    log::info!("Mombasa v{} initializing", env!("CARGO_PKG_VERSION"));

    std::thread::spawn(|| {
        log::info!("spawned gRPC thread");
        grpc_startup();
    });
}

fn setup_logging() -> Result<LoggerHandle, FlexiLoggerError> {
    let user_dirs = UserDirs::new();

    let log_dir = user_dirs.desktop_dir();

    Logger::try_with_str("info")?
        //.log_to_file(FileSpec::default()
        //   .directory(log_dir.or(Some(std::path::Path::new("C:\\mombasa"))).unwrap())
        //   .basename("mombasa")
        //   .suppress_timestamp()
        //   .suffix("proclog"))
        //.append()
        .format(detailed_format)
        .start()
}

#[no_mangle]
pub extern "C" fn teardown() {
    log::info!("teardown signaled");
    let s = SHUTDOWN.get();
    if s.is_some() {
        log::info!("shutdown trigger found");
        s.unwrap().trigger();
        log::info!("shutdown triggered");
    }
}

#[no_mangle]
pub extern "C" fn grpc_startup() {
    
    log::info!("grpc_startup running");

    let runtime = tokio::runtime::Builder::new_multi_thread()
        .worker_threads(1)
        .thread_name("mombasa-worker")
        .thread_stack_size(3 * 1024 * 1024)
        .enable_all()
        .build()
        .unwrap();

    log::info!("gRPC runtime created");

    runtime.block_on(async {
        let addr = "127.0.0.1:50051".parse().unwrap();
        let bridge = Bridge::default();
        let (trigger, listener) = triggered::trigger();
        let _ = SHUTDOWN.set(trigger);

        log::info!("starting grpc server");

        Server::builder()
            .add_service(MombasaBridgeServer::new(bridge))
            .serve_with_shutdown(addr, listener)
            .await
            .unwrap();
    });

    log::info!("gRPC thread should be ending");
}