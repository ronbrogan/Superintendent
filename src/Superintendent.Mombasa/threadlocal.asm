_TEXT SEGMENT

; void setthreadlocal(uint64 value);
PUBLIC setthreadlocal
setthreadlocal PROC FRAME
.endprolog

mov gs:[58h], rcx
ret

setthreadlocal ENDP
END