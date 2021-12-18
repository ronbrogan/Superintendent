use core::arch::asm;
use tonic::{Request, Response, Status};
use std::slice;
use std::time::Instant;

use crate::mombasa_bridge::mombasa_bridge_server::{MombasaBridge};
use crate::mombasa_bridge::{CallRequest, CallResponse};
use crate::mombasa_bridge::{MemoryReadRequest, MemoryReadResponse};
use crate::mombasa_bridge::{MemoryWriteRequest, MemoryWriteResponse};
use crate::mombasa_bridge::{MemoryAllocateRequest, MemoryAllocateResponse};
use crate::mombasa_bridge::{MemoryFreeRequest, MemoryFreeResponse};

#[derive(Debug, Default)]
pub struct Bridge {}

#[tonic::async_trait]
impl MombasaBridge for Bridge {
    async fn call_function(&self, request: Request<CallRequest>) -> Result<Response<CallResponse>, Status> {
        let start = Instant::now();

        let req = request.into_inner();

        let fnptr = req.function_pointer;
        let mut arg0 : u64 = 0;
        let mut arg1 : u64 = 0;
        let mut arg2 : u64= 0;
        let mut arg3 : u64= 0;
        let mut retval: u64;
        let mut retfloat: u64;

        let len = req.args.len();

        if len >= 1 {
            arg0 = req.args[0];
        }

        if len >= 2 {
            arg1 = req.args[1];
        }

        if len >= 3 {
            arg2 = req.args[2];
        }

        if len >= 4 {
            arg3 = req.args[3];
        }

        unsafe {
            if len > 4 {
                // TODO: support more than 4 args?
                // push remaining args in reverse to the stack
                //for n in (4..len).rev() {
                //    let v = req.args[n];
                //}
            }

            // Setup registers, make the call, indicate clobbered registers
            asm!(
                "call {0}",
                in(reg) fnptr,
                in("rcx") arg0,
                in("rdx") arg1,
                in("r8") arg2,
                in("r9") arg3,
                inout("xmm0") arg0 => retfloat,
                in("xmm1") arg1,
                in("xmm2") arg2,
                in("xmm3") arg3,
                out("rax") retval,
                out("r10") _,
                out("r11") _,
                out("xmm4") _,
                out("xmm5") _
            );
        }

        // we might need to separate return values if int/float ORing doesn't work
        let reply = CallResponse {
            duration_microseconds: start.elapsed().as_micros() as u64,
            value: if req.returns_float { retfloat } else { retval }
        };

        Ok(Response::new(reply))
    }

    async fn read_memory(&self, request: Request<MemoryReadRequest>) -> Result<Response<MemoryReadResponse>, Status> {
        let start = Instant::now();
        let req = request.into_inner();

        let ptr = req.address as *const u8;
        assert!(!ptr.is_null());

        let data: Vec<u8>;

        unsafe {
            data = slice::from_raw_parts(ptr, req.count as usize).to_vec();
        }

        let reply = MemoryReadResponse {
            duration_microseconds: start.elapsed().as_micros() as u64,
            address: req.address,
            data: data
        };

        Ok(Response::new(reply))
    }

    async fn write_memory(&self, request: Request<MemoryWriteRequest>) -> Result<Response<MemoryWriteResponse>, Status> {
        let start = Instant::now();
        let req = request.into_inner();

        if req.data.len() == 0 { return Ok(Response::new(MemoryWriteResponse { duration_microseconds: start.elapsed().as_micros() as u64 })); }

        let dst = req.address as *mut libc::c_void;
        assert!(!dst.is_null());

        unsafe {
            libc::memcpy(dst, req.data.as_ptr() as *const libc::c_void, req.data.len());
        }

        Ok(Response::new(MemoryWriteResponse { duration_microseconds: start.elapsed().as_micros() as u64 }))
    }

    async fn allocate_memory(&self, request: Request<MemoryAllocateRequest>) -> Result<Response<MemoryAllocateResponse>, Status> {
        let start = Instant::now();
        let req = request.into_inner();

        let commit = 0x1000;
        let allocated:u64;

        unsafe {
            allocated = winapi::um::memoryapi::VirtualAlloc(0 as *mut libc::c_void, req.length as usize, commit, req.protection) as u64;
        }

        let reply = MemoryAllocateResponse {
            duration_microseconds: start.elapsed().as_micros() as u64,
            address: allocated as u64
        };

        Ok(Response::new(reply))
    }

    async fn free_memory(&self, request: Request<MemoryFreeRequest>) -> Result<Response<MemoryFreeResponse>, Status> {
        let start = Instant::now();
        let req = request.into_inner();
        
        unsafe {
            winapi::um::memoryapi::VirtualFree(req.address as *mut libc::c_void, 0, req.free_type);
        }

        Ok(Response::new(MemoryFreeResponse { duration_microseconds: start.elapsed().as_micros() as u64 }))
    }
}