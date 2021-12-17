use mombasa_bridge::mombasa_bridge_client::MombasaBridgeClient;
use mombasa_bridge::CallRequest;

pub mod mombasa_bridge {
    tonic::include_proto!("mombasa");
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut client = MombasaBridgeClient::connect("http://127.0.0.1:50051").await?;

    let request = tonic::Request::new(CallRequest {
        function_pointer: 140718941578336,
        args: Vec::new(),
        returns_float: false
    });

    let response = client.call_function(request).await?;

    println!("RESPONSE={:?}", response);

    Ok(())
}