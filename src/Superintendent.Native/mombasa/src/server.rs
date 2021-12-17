#![feature(asm)]

use tonic::{transport::Server};

mod server_lib;
use server_lib::Bridge;

mod mombasa_bridge { tonic::include_proto!("mombasa"); }
use mombasa_bridge::mombasa_bridge_server::MombasaBridgeServer;

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let addr = "127.0.0.1:50051".parse()?;
    let greeter = Bridge::default();

    Server::builder()
        .add_service(MombasaBridgeServer::new(greeter))
        .serve(addr)
        .await?;

    Ok(())
}