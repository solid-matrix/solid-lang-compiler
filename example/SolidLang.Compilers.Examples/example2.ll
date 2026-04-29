; ModuleID = 'solid_module'
source_filename = "solid_module"
target triple = "x86_64-pc-windows-msvc"

define i32 @factorial(i32 %0) {
entry:
  %n = alloca i32, align 4
  store i32 %0, ptr %n, align 4
  %result = alloca i32, align 4
  store i32 1, ptr %result, align 4
  %i = alloca i32, align 4
  store i32 1, ptr %i, align 4
  br label %whilecond

whilecond:                                        ; preds = %whilebody, %entry
  %i1 = load i32, ptr %i, align 4
  %n2 = load i32, ptr %n, align 4
  %letmp = icmp sle i32 %i1, %n2
  %whilecond3 = icmp ne i1 %letmp, false
  br i1 %whilecond3, label %whilebody, label %whilecont

whilebody:                                        ; preds = %whilecond
  %result4 = load i32, ptr %result, align 4
  %i5 = load i32, ptr %i, align 4
  %multmp = mul i32 %result4, %i5
  store i32 %multmp, ptr %result, align 4
  %i6 = load i32, ptr %i, align 4
  %addtmp = add i32 %i6, 1
  store i32 %addtmp, ptr %i, align 4
  br label %whilecond

whilecont:                                        ; preds = %whilecond
  %result7 = load i32, ptr %result, align 4
  ret i32 %result7
}

define i32 @fibonacci(i32 %0) {
entry:
  %n = alloca i32, align 4
  store i32 %0, ptr %n, align 4
  %n1 = load i32, ptr %n, align 4
  %letmp = icmp sle i32 %n1, 1
  %ifcond = icmp ne i1 %letmp, false
  br i1 %ifcond, label %then, label %else

then:                                             ; preds = %entry
  %n2 = load i32, ptr %n, align 4
  ret i32 %n2

else:                                             ; preds = %entry
  br label %ifcont

ifcont:                                           ; preds = %else
  %a = alloca i32, align 4
  store i32 0, ptr %a, align 4
  %b = alloca i32, align 4
  store i32 1, ptr %b, align 4
  %i = alloca i32, align 4
  store i32 2, ptr %i, align 4
  br label %whilecond

whilecond:                                        ; preds = %whilebody, %ifcont
  %i3 = load i32, ptr %i, align 4
  %n4 = load i32, ptr %n, align 4
  %letmp5 = icmp sle i32 %i3, %n4
  %whilecond6 = icmp ne i1 %letmp5, false
  br i1 %whilecond6, label %whilebody, label %whilecont

whilebody:                                        ; preds = %whilecond
  %temp = alloca i32, align 4
  %a7 = load i32, ptr %a, align 4
  %b8 = load i32, ptr %b, align 4
  %addtmp = add i32 %a7, %b8
  store i32 %addtmp, ptr %temp, align 4
  %b9 = load i32, ptr %b, align 4
  store i32 %b9, ptr %a, align 4
  %temp10 = load i32, ptr %temp, align 4
  store i32 %temp10, ptr %b, align 4
  %i11 = load i32, ptr %i, align 4
  %addtmp12 = add i32 %i11, 1
  store i32 %addtmp12, ptr %i, align 4
  br label %whilecond

whilecont:                                        ; preds = %whilecond
  %b13 = load i32, ptr %b, align 4
  ret i32 %b13
}

define i32 @sum_to(i32 %0) {
entry:
  %n = alloca i32, align 4
  store i32 %0, ptr %n, align 4
  %sum = alloca i32, align 4
  store i32 0, ptr %sum, align 4
  %i = alloca i32, align 4
  store i32 1, ptr %i, align 4
  br label %forcond

forcond:                                          ; preds = %forbody, %entry
  %i1 = load i32, ptr %i, align 4
  %n2 = load i32, ptr %n, align 4
  %letmp = icmp sle i32 %i1, %n2
  %forcond3 = icmp ne i1 %letmp, false
  br i1 %forcond3, label %forbody, label %forcont

forbody:                                          ; preds = %forcond
  %sum4 = load i32, ptr %sum, align 4
  %i5 = load i32, ptr %i, align 4
  %addtmp = add i32 %sum4, %i5
  store i32 %addtmp, ptr %sum, align 4
  %i6 = load i32, ptr %i, align 4
  %addtmp7 = add i32 %i6, 1
  store i32 %addtmp7, ptr %i, align 4
  br label %forcond

forcont:                                          ; preds = %forcond
  %sum8 = load i32, ptr %sum, align 4
  ret i32 %sum8
}

define i32 @main() {
entry:
  %fact5 = alloca i32, align 4
  %calltmp = call i32 @factorial(i32 5)
  store i32 %calltmp, ptr %fact5, align 4
  %fib10 = alloca i32, align 4
  %calltmp1 = call i32 @fibonacci(i32 10)
  store i32 %calltmp1, ptr %fib10, align 4
  %sum10 = alloca i32, align 4
  %calltmp2 = call i32 @sum_to(i32 10)
  store i32 %calltmp2, ptr %sum10, align 4
  %fib103 = load i32, ptr %fib10, align 4
  ret i32 %fib103
}
