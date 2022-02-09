_TEXT SEGMENT

; uint64 function_dispatcher(void* function, uint64* argv, bool isFloat);
PUBLIC function_dispatcher
function_dispatcher PROC FRAME
push rbp ; save previous frame pointer
.pushreg rbp ; encode unwind info
mov rbp, rsp ; set new frame pointer
.setframe rbp, 0 ; encode frame pointer
.endprolog

; prepare stack
push rsi
push rbx
sub rsp, 80h

mov r10, rcx ; function pointer
mov rsi, rdx ; arg array, 12 elements
mov r11, r8  ; if eax should be replaced with xmm0

; standard args
mov rcx, qword ptr [rsi + 00h]
movq xmm0, rcx
mov rdx, qword ptr [rsi + 08h]
movq xmm1, rdx
mov r8,  qword ptr [rsi + 10h]
movq xmm2, r8
mov r9,  qword ptr [rsi + 18h]
movq xmm3, r9

; stack args
mov rbx, qword ptr [rsi + 20h]
mov qword ptr [rsp + 20h], rbx

mov rbx, qword ptr [rsi + 28h]
mov qword ptr [rsp + 28h], rbx

mov rbx, qword ptr [rsi + 30h]
mov qword ptr [rsp + 30h], rbx

mov rbx, qword ptr [rsi + 38h]
mov qword ptr [rsp + 38h], rbx

mov rbx, qword ptr [rsi + 40h]
mov qword ptr [rsp + 40h], rbx

mov rbx, qword ptr [rsi + 48h]
mov qword ptr [rsp + 48h], rbx

mov rbx, qword ptr [rsi + 50h]
mov qword ptr [rsp + 50h], rbx

mov rbx, qword ptr [rsi + 58h]
mov qword ptr [rsp + 58h], rbx

; make the call
call r10

; check if we should move float result into return value
test r11, r11
je done
movq rax, xmm0

;restore stack
done:
add rsp, 80h
pop rbx
pop rsi
pop rbp

ret

function_dispatcher ENDP

_TEXT ENDS
END
