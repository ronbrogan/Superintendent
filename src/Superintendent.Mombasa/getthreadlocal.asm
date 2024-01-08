_TEXT SEGMENT

; uint64 getthreadlocal();
PUBLIC getthreadlocal
getthreadlocal PROC FRAME
.endprolog

mov rax, gs:[58h]
ret

getthreadlocal ENDP

_TEXT ENDS
END
