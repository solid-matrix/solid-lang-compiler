; ModuleID = 'solid_module'
source_filename = "solid_module"
target triple = "x86_64-pc-windows-msvc"

define i32 @factorial_rec(i32 %0) {
entry:
  %n = alloca i32, align 4
  store i32 %0, ptr %n, align 4
  %n1 = load i32, ptr %n, align 4
  %letmp = icmp sle i32 %n1, 1
  %ifcond = icmp ne i1 %letmp, false
  br i1 %ifcond, label %then, label %else

then:                                             ; preds = %entry
  ret i32 1

else:                                             ; preds = %entry
  br label %ifcont

ifcont:                                           ; preds = %else
  %n2 = load i32, ptr %n, align 4
  %n3 = load i32, ptr %n, align 4
  %subtmp = sub i32 %n3, 1
  %calltmp = call i32 @factorial_rec(i32 %subtmp)
  %multmp = mul i32 %n2, %calltmp
  ret i32 %multmp
}

define i32 @fib_rec(i32 %0) {
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
  %n3 = load i32, ptr %n, align 4
  %subtmp = sub i32 %n3, 1
  %calltmp = call i32 @fib_rec(i32 %subtmp)
  %n4 = load i32, ptr %n, align 4
  %subtmp5 = sub i32 %n4, 2
  %calltmp6 = call i32 @fib_rec(i32 %subtmp5)
  %addtmp = add i32 %calltmp, %calltmp6
  ret i32 %addtmp
}

define i32 @abs(i32 %0) {
entry:
  %x = alloca i32, align 4
  store i32 %0, ptr %x, align 4
  %x1 = load i32, ptr %x, align 4
  %lttmp = icmp slt i32 %x1, 0
  %x2 = load i32, ptr %x, align 4
  %subtmp = sub i32 0, %x2
  %x3 = load i32, ptr %x, align 4
  %condtmp = select i1 %lttmp, i32 %subtmp, i32 %x3
  ret i32 %condtmp
}

define i32 @max(i32 %0, i32 %1) {
entry:
  %a = alloca i32, align 4
  store i32 %0, ptr %a, align 4
  %b = alloca i32, align 4
  store i32 %1, ptr %b, align 4
  %a1 = load i32, ptr %a, align 4
  %b2 = load i32, ptr %b, align 4
  %gttmp = icmp sgt i32 %a1, %b2
  %a3 = load i32, ptr %a, align 4
  %b4 = load i32, ptr %b, align 4
  %condtmp = select i1 %gttmp, i32 %a3, i32 %b4
  ret i32 %condtmp
}

define i32 @min(i32 %0, i32 %1) {
entry:
  %a = alloca i32, align 4
  store i32 %0, ptr %a, align 4
  %b = alloca i32, align 4
  store i32 %1, ptr %b, align 4
  %a1 = load i32, ptr %a, align 4
  %b2 = load i32, ptr %b, align 4
  %lttmp = icmp slt i32 %a1, %b2
  %a3 = load i32, ptr %a, align 4
  %b4 = load i32, ptr %b, align 4
  %condtmp = select i1 %lttmp, i32 %a3, i32 %b4
  ret i32 %condtmp
}

define i32 @sign(i32 %0) {
entry:
  %x = alloca i32, align 4
  store i32 %0, ptr %x, align 4
  %x1 = load i32, ptr %x, align 4
  %lttmp = icmp slt i32 %x1, 0
  %x2 = load i32, ptr %x, align 4
  %eqtmp = icmp eq i32 %x2, 0
  %condtmp = select i1 %eqtmp, i32 0, i32 1
  %condtmp3 = select i1 %lttmp, i32 -1, i32 %condtmp
  ret i32 %condtmp3
}

define i32 @main() {
entry:
  %f5 = alloca i32, align 4
  %calltmp = call i32 @factorial_rec(i32 5)
  store i32 %calltmp, ptr %f5, align 4
  %fib10 = alloca i32, align 4
  %calltmp1 = call i32 @fib_rec(i32 10)
  store i32 %calltmp1, ptr %fib10, align 4
  %a = alloca i32, align 4
  %calltmp2 = call i32 @abs(i32 -7)
  store i32 %calltmp2, ptr %a, align 4
  %m = alloca i32, align 4
  %calltmp3 = call i32 @max(i32 3, i32 8)
  store i32 %calltmp3, ptr %m, align 4
  %s1 = alloca i32, align 4
  %calltmp4 = call i32 @sign(i32 -5)
  store i32 %calltmp4, ptr %s1, align 4
  %s2 = alloca i32, align 4
  %calltmp5 = call i32 @sign(i32 0)
  store i32 %calltmp5, ptr %s2, align 4
  %s3 = alloca i32, align 4
  %calltmp6 = call i32 @sign(i32 5)
  store i32 %calltmp6, ptr %s3, align 4
  %f57 = load i32, ptr %f5, align 4
  %fib108 = load i32, ptr %fib10, align 4
  %addtmp = add i32 %f57, %fib108
  ret i32 %addtmp
}
