use mombasa_bridge::mombasa_bridge_client::MombasaBridgeClient;
use mombasa_bridge::{CallRequest,MemoryReadRequest};

pub mod mombasa_bridge {
    tonic::include_proto!("mombasa");
}

#[tokio::main]
async fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut client = MombasaBridgeClient::connect("http://127.0.0.1:50051").await?;

    let request2 = tonic::Request::new(MemoryReadRequest {
        address: 0x7FF7C027F650,
        count: 14
    });

    let response2 = client.read_memory(request2).await?;

    println!("RESPONSE={:?}", response2);
    println!("{}", std::str::from_utf8(&response2.into_inner().data).unwrap());


    let request = tonic::Request::new(CallRequest {
        function_pointer: 0x7FF7C018B502,
        args: Vec::new(),
        returns_float: false
    });

    let response = client.call_function(request).await?;

    println!("RESPONSE={:?}", response);

    let request3 = tonic::Request::new(CallRequest {
        function_pointer: 0x7FF7502D4680,
        args: vec![1,2,3,4,5],
        returns_float: false
    });

    let response3 = client.call_function(request3).await?;

    println!("RESPONSE={:?}", response3);

    

    
    Ok(())
}