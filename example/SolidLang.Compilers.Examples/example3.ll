; ModuleID = 'solid_module'
source_filename = "solid_module"
target triple = "x86_64-pc-windows-msvc"

define i32 @is_prime(i32 %0) {
entry:
  %n = alloca i32, align 4
  store i32 %0, ptr %n, align 4
  %n1 = load i32, ptr %n, align 4
  %letmp = icmp sle i32 %n1, 1
  %ifcond = icmp ne i1 %letmp, false
  br i1 %ifcond, label %then, label %else

then:                                             ; preds = %entry
  ret i32 0

else:                                             ; preds = %entry
  br label %ifcont

ifcont:                                           ; preds = %else
  %n2 = load i32, ptr %n, align 4
  %letmp3 = icmp sle i32 %n2, 3
  %ifcond4 = icmp ne i1 %letmp3, false
  br i1 %ifcond4, label %then5, label %else6

then5:                                            ; preds = %ifcont
  ret i32 1

else6:                                            ; preds = %ifcont
  br label %ifcont7

ifcont7:                                          ; preds = %else6
  %n8 = load i32, ptr %n, align 4
  %modtmp = srem i32 %n8, 2
  %eqtmp = icmp eq i32 %modtmp, 0
  %ifcond9 = icmp ne i1 %eqtmp, false
  br i1 %ifcond9, label %then10, label %else11

then10:                                           ; preds = %ifcont7
  ret i32 0

else11:                                           ; preds = %ifcont7
  br label %ifcont12

ifcont12:                                         ; preds = %else11
  %i = alloca i32, align 4
  store i32 3, ptr %i, align 4
  br label %whilecond

whilecond:                                        ; preds = %ifcont25, %ifcont12
  %i13 = load i32, ptr %i, align 4
  %i14 = load i32, ptr %i, align 4
  %multmp = mul i32 %i13, %i14
  %n15 = load i32, ptr %n, align 4
  %letmp16 = icmp sle i32 %multmp, %n15
  %whilecond17 = icmp ne i1 %letmp16, false
  br i1 %whilecond17, label %whilebody, label %whilecont

whilebody:                                        ; preds = %whilecond
  %n18 = load i32, ptr %n, align 4
  %i19 = load i32, ptr %i, align 4
  %modtmp20 = srem i32 %n18, %i19
  %eqtmp21 = icmp eq i32 %modtmp20, 0
  %ifcond22 = icmp ne i1 %eqtmp21, false
  br i1 %ifcond22, label %then23, label %else24

whilecont:                                        ; preds = %whilecond
  ret i32 1

then23:                                           ; preds = %whilebody
  ret i32 0

else24:                                           ; preds = %whilebody
  br label %ifcont25

ifcont25:                                         ; preds = %else24
  %i26 = load i32, ptr %i, align 4
  %addtmp = add i32 %i26, 2
  store i32 %addtmp, ptr %i, align 4
  br label %whilecond
}

define i32 @next_prime(i32 %0) {
entry:
  %start = alloca i32, align 4
  store i32 %0, ptr %start, align 4
  %n = alloca i32, align 4
  %start1 = load i32, ptr %start, align 4
  %addtmp = add i32 %start1, 1
  store i32 %addtmp, ptr %n, align 4
  br label %whilecond

whilecond:                                        ; preds = %ifcont, %entry
  br i1 true, label %whilebody, label %whilecont

whilebody:                                        ; preds = %whilecond
  %n2 = load i32, ptr %n, align 4
  %calltmp = call i32 @is_prime(i32 %n2)
  %eqtmp = icmp eq i32 %calltmp, 1
  %ifcond = icmp ne i1 %eqtmp, false
  br i1 %ifcond, label %then, label %else

whilecont:                                        ; preds = %whilecond
  ret i32 0

then:                                             ; preds = %whilebody
  %n3 = load i32, ptr %n, align 4
  ret i32 %n3

else:                                             ; preds = %whilebody
  br label %ifcont

ifcont:                                           ; preds = %else
  %n4 = load i32, ptr %n, align 4
  %addtmp5 = add i32 %n4, 1
  store i32 %addtmp5, ptr %n, align 4
  br label %whilecond
}

define i32 @sum_primes(i32 %0) {
entry:
  %count = alloca i32, align 4
  store i32 %0, ptr %count, align 4
  %sum = alloca i32, align 4
  store i32 0, ptr %sum, align 4
  %num = alloca i32, align 4
  store i32 2, ptr %num, align 4
  %cnt = alloca i32, align 4
  store i32 0, ptr %cnt, align 4
  br label %whilecond

whilecond:                                        ; preds = %ifcont, %entry
  %cnt1 = load i32, ptr %cnt, align 4
  %count2 = load i32, ptr %count, align 4
  %lttmp = icmp slt i32 %cnt1, %count2
  %whilecond3 = icmp ne i1 %lttmp, false
  br i1 %whilecond3, label %whilebody, label %whilecont

whilebody:                                        ; preds = %whilecond
  %num4 = load i32, ptr %num, align 4
  %calltmp = call i32 @is_prime(i32 %num4)
  %eqtmp = icmp eq i32 %calltmp, 1
  %ifcond = icmp ne i1 %eqtmp, false
  br i1 %ifcond, label %then, label %else

whilecont:                                        ; preds = %whilecond
  %sum11 = load i32, ptr %sum, align 4
  ret i32 %sum11

then:                                             ; preds = %whilebody
  %sum5 = load i32, ptr %sum, align 4
  %num6 = load i32, ptr %num, align 4
  %addtmp = add i32 %sum5, %num6
  store i32 %addtmp, ptr %sum, align 4
  %cnt7 = load i32, ptr %cnt, align 4
  %addtmp8 = add i32 %cnt7, 1
  store i32 %addtmp8, ptr %cnt, align 4
  br label %ifcont

else:                                             ; preds = %whilebody
  br label %ifcont

ifcont:                                           ; preds = %else, %then
  %num9 = load i32, ptr %num, align 4
  %addtmp10 = add i32 %num9, 1
  store i32 %addtmp10, ptr %num, align 4
  br label %whilecond
}

define i32 @find_divisible_by_7(i32 %0) {
entry:
  %limit = alloca i32, align 4
  store i32 %0, ptr %limit, align 4
  %i = alloca i32, align 4
  store i32 1, ptr %i, align 4
  br label %whilecond

whilecond:                                        ; preds = %ifcont, %entry
  %i1 = load i32, ptr %i, align 4
  %limit2 = load i32, ptr %limit, align 4
  %letmp = icmp sle i32 %i1, %limit2
  %whilecond3 = icmp ne i1 %letmp, false
  br i1 %whilecond3, label %whilebody, label %whilecont

whilebody:                                        ; preds = %whilecond
  %i4 = load i32, ptr %i, align 4
  %modtmp = srem i32 %i4, 7
  %eqtmp = icmp eq i32 %modtmp, 0
  %ifcond = icmp ne i1 %eqtmp, false
  br i1 %ifcond, label %then, label %else

whilecont:                                        ; preds = %whilecond
  ret i32 0

then:                                             ; preds = %whilebody
  %i5 = load i32, ptr %i, align 4
  ret i32 %i5

else:                                             ; preds = %whilebody
  br label %ifcont

ifcont:                                           ; preds = %else
  %i6 = load i32, ptr %i, align 4
  %addtmp = add i32 %i6, 1
  store i32 %addtmp, ptr %i, align 4
  br label %whilecond
}

define i32 @sum_evens_with_skip(i32 %0, i32 %1) {
entry:
  %start = alloca i32, align 4
  store i32 %0, ptr %start, align 4
  %end = alloca i32, align 4
  store i32 %1, ptr %end, align 4
  %sum = alloca i32, align 4
  store i32 0, ptr %sum, align 4
  %i = alloca i32, align 4
  %start1 = load i32, ptr %start, align 4
  store i32 %start1, ptr %i, align 4
  br label %whilecond

whilecond:                                        ; preds = %ifcont, %entry
  %i2 = load i32, ptr %i, align 4
  %end3 = load i32, ptr %end, align 4
  %letmp = icmp sle i32 %i2, %end3
  %whilecond4 = icmp ne i1 %letmp, false
  br i1 %whilecond4, label %whilebody, label %whilecont

whilebody:                                        ; preds = %whilecond
  %i5 = load i32, ptr %i, align 4
  %modtmp = srem i32 %i5, 2
  %netmp = icmp ne i32 %modtmp, 0
  %ifcond = icmp ne i1 %netmp, false
  br i1 %ifcond, label %then, label %else

whilecont:                                        ; preds = %whilecond
  %sum12 = load i32, ptr %sum, align 4
  ret i32 %sum12

then:                                             ; preds = %whilebody
  %i6 = load i32, ptr %i, align 4
  %addtmp = add i32 %i6, 1
  store i32 %addtmp, ptr %i, align 4
  br label %ifcont

else:                                             ; preds = %whilebody
  %sum7 = load i32, ptr %sum, align 4
  %i8 = load i32, ptr %i, align 4
  %addtmp9 = add i32 %sum7, %i8
  store i32 %addtmp9, ptr %sum, align 4
  %i10 = load i32, ptr %i, align 4
  %addtmp11 = add i32 %i10, 1
  store i32 %addtmp11, ptr %i, align 4
  br label %ifcont

ifcont:                                           ; preds = %else, %then
  br label %whilecond
}

define i32 @main() {
entry:
  %prime_check = alloca i32, align 4
  %calltmp = call i32 @is_prime(i32 17)
  store i32 %calltmp, ptr %prime_check, align 4
  %prime_sum = alloca i32, align 4
  %calltmp1 = call i32 @sum_primes(i32 10)
  store i32 %calltmp1, ptr %prime_sum, align 4
  %div7 = alloca i32, align 4
  %calltmp2 = call i32 @find_divisible_by_7(i32 100)
  store i32 %calltmp2, ptr %div7, align 4
  %even_sum = alloca i32, align 4
  %calltmp3 = call i32 @sum_evens_with_skip(i32 10, i32 20)
  store i32 %calltmp3, ptr %even_sum, align 4
  %prime_sum4 = load i32, ptr %prime_sum, align 4
  ret i32 %prime_sum4
}
