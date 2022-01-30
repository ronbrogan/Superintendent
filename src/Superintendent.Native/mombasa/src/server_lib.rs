use core::arch::asm;
use tonic::{Request, Response, Status};
use std::slice;
use std::time::Instant;
use std::panic;
use std::any::Any;
use std::marker::Send;

use crate::mombasa_bridge::mombasa_bridge_server::{MombasaBridge};
use crate::mombasa_bridge::{CallRequest, CallResponse};
use crate::mombasa_bridge::{MemoryReadRequest, MemoryReadResponse};
use crate::mombasa_bridge::{MemoryWriteRequest, MemoryWriteResponse};
use crate::mombasa_bridge::{MemoryAllocateRequest, MemoryAllocateResponse};
use crate::mombasa_bridge::{MemoryFreeRequest, MemoryFreeResponse};
use crate::mombasa_bridge::{SetTlsValueRequest, SetTlsValueResponse};

#[derive(Debug, Default)]
pub struct Bridge {}

impl Bridge {
    fn call_function_impl(fnptr: u64, arg0: u64, arg1: u64, arg2: u64, arg3: u64, returns_float: bool) -> Result<u64, Box<dyn Any+Send>> {
        log::info!("Calling function at {}", fnptr);
        
        return panic::catch_unwind(|| {
            let mut retval: u64;
            let mut retfloat: u64;

            unsafe {
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
                    clobber_abi("win64"),
                );
            }

            if returns_float { retfloat } else { retval }
        });
    }

    fn call_function_impl_extended(fnptr: u64, arg0: u64, arg1: u64, arg2: u64, arg3: u64, args: [u64; 8], returns_float: bool) -> Result<u64, Box<dyn Any+Send>> {
        log::info!("Calling extended function at {}", fnptr);

        return panic::catch_unwind(|| {
            let mut retval: u64;
            let mut retfloat: u64;

            unsafe {
                // Setup registers, push args to stack, make the call, indicate clobbered registers
                asm!(
                    "sub rsp, 60h", // asumption is that rust's asm! will have already reserved 32 bytes for the call
                    "mov qword ptr [rsp+20h], {1}",
                    "mov qword ptr [rsp+28h], {2}",
                    "mov qword ptr [rsp+30h], {3}",
                    "mov qword ptr [rsp+38h], {4}",
                    "mov qword ptr [rsp+40h], {5}",
                    "mov qword ptr [rsp+48h], {6}",
                    "mov qword ptr [rsp+50h], {7}",
                    "mov qword ptr [rsp+58h], {8}",
                    "call {0}",
                    "add rsp, 60h",
                    in(reg) fnptr,
                    in(reg) args[0],
                    in(reg) args[1],
                    in(reg) args[2],
                    in(reg) args[3],
                    in(reg) args[4],
                    in(reg) args[5],
                    in(reg) args[6],
                    in(reg) args[7],
                    in("rcx") arg0,
                    in("rdx") arg1,
                    in("r8") arg2,
                    in("r9") arg3,
                    inout("xmm0") arg0 => retfloat,
                    in("xmm1") arg1,
                    in("xmm2") arg2,
                    in("xmm3") arg3,
                    out("rax") retval,
                    clobber_abi("win64"),
                );
            }

            if returns_float { retfloat } else { retval }
        });
    }
}

#[tonic::async_trait]
impl MombasaBridge for Bridge {
    async fn call_function(&self, request: Request<CallRequest>) -> Result<Response<CallResponse>, Status> {
        let start = Instant::now();

        let req = request.into_inner();

        let mut arg0 : u64 = 0;
        let mut arg1 : u64 = 0;
        let mut arg2 : u64= 0;
        let mut arg3 : u64= 0;

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

        let mut result: u64 = 0;
        let mut success = true;

        if len <= 4 {
            match Bridge::call_function_impl(req.function_pointer, arg0, arg1, arg2, arg3, req.returns_float) {
                Ok(i) => result = i,
                Err(_) => success = false
            }
        } else {
            // push remaining args into array
            let mut extra_index = 0;
            let mut extra_args: [u64; 8]= [0; 8];
            for n in 4..len {
                if extra_index < extra_args.len() {
                    extra_args[extra_index] = req.args[n];
                }

                extra_index += 1;
            }

            match Bridge::call_function_impl_extended(req.function_pointer, arg0, arg1, arg2, arg3, extra_args, req.returns_float) {
                Ok(i) => result = i,
                Err(_) => success = false
            }
        }

        // we might need to separate return values if int/float ORing doesn't work
        let reply = CallResponse {
            duration_microseconds: start.elapsed().as_micros() as u64,
            success: success,
            value: result
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

        log::info!("Allocating memory of length {} bytes", req.length);

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

        log::info!("Freeing memory at {}", req.address);

        unsafe {
            winapi::um::memoryapi::VirtualFree(req.address as *mut libc::c_void, 0, req.free_type);
        }

        Ok(Response::new(MemoryFreeResponse { duration_microseconds: start.elapsed().as_micros() as u64 }))
    }

    async fn set_tls_value(&self, request: Request<SetTlsValueRequest>) -> Result<Response<SetTlsValueResponse>, Status> {

        let start = Instant::now();
        let req = request.into_inner();
        
        log::info!("Setting tls {}/{}", req.index, req.value);

        unsafe {
            winapi::um::processthreadsapi::TlsSetValue(req.index, req.value as *mut libc::c_void);
        }

        Ok(Response::new(SetTlsValueResponse { duration_microseconds: start.elapsed().as_micros() as u64 }))
    }
}