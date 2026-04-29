; ModuleID = 'solid_module'
source_filename = "solid_module"
target triple = "x86_64-pc-windows-msvc"

define i32 @day_name(i32 %0) {
entry:
  %day = alloca i32, align 4
  store i32 %0, ptr %day, align 4
  %day1 = load i32, ptr %day, align 4
  switch i32 %day1, label %switch.default [
    i32 1, label %switch.case
    i32 2, label %switch.case2
    i32 3, label %switch.case3
    i32 4, label %switch.case4
    i32 5, label %switch.case5
    i32 6, label %switch.case6
    i32 7, label %switch.case7
  ]

switch.default:                                   ; preds = %entry
  ret i32 0

switch.after:                                     ; No predecessors!
  ret i32 0

switch.case:                                      ; preds = %entry
  ret i32 1

switch.case2:                                     ; preds = %entry
  ret i32 2

switch.case3:                                     ; preds = %entry
  ret i32 3

switch.case4:                                     ; preds = %entry
  ret i32 4

switch.case5:                                     ; preds = %entry
  ret i32 5

switch.case6:                                     ; preds = %entry
  ret i32 6

switch.case7:                                     ; preds = %entry
  ret i32 7
}

define i32 @calc(i32 %0, i32 %1, i32 %2) {
entry:
  %op = alloca i32, align 4
  store i32 %0, ptr %op, align 4
  %a = alloca i32, align 4
  store i32 %1, ptr %a, align 4
  %b = alloca i32, align 4
  store i32 %2, ptr %b, align 4
  %op1 = load i32, ptr %op, align 4
  switch i32 %op1, label %switch.default [
    i32 1, label %switch.case
    i32 2, label %switch.case2
    i32 3, label %switch.case3
    i32 4, label %switch.case4
  ]

switch.default:                                   ; preds = %entry
  ret i32 0

switch.after:                                     ; No predecessors!
  ret i32 0

switch.case:                                      ; preds = %entry
  %a5 = load i32, ptr %a, align 4
  %b6 = load i32, ptr %b, align 4
  %addtmp = add i32 %a5, %b6
  ret i32 %addtmp

switch.case2:                                     ; preds = %entry
  %a7 = load i32, ptr %a, align 4
  %b8 = load i32, ptr %b, align 4
  %subtmp = sub i32 %a7, %b8
  ret i32 %subtmp

switch.case3:                                     ; preds = %entry
  %a9 = load i32, ptr %a, align 4
  %b10 = load i32, ptr %b, align 4
  %multmp = mul i32 %a9, %b10
  ret i32 %multmp

switch.case4:                                     ; preds = %entry
  %a11 = load i32, ptr %a, align 4
  %b12 = load i32, ptr %b, align 4
  %divtmp = sdiv i32 %a11, %b12
  ret i32 %divtmp
}

define i32 @piecewise(i32 %0) {
entry:
  %x = alloca i32, align 4
  store i32 %0, ptr %x, align 4
  %x1 = load i32, ptr %x, align 4
  %lttmp = icmp slt i32 %x1, 0
  %ifcond = icmp ne i1 %lttmp, false
  br i1 %ifcond, label %then, label %else

then:                                             ; preds = %entry
  %x2 = load i32, ptr %x, align 4
  %subtmp = sub i32 0, %x2
  ret i32 %subtmp

else:                                             ; preds = %entry
  br label %ifcont

ifcont:                                           ; preds = %else
  %x3 = load i32, ptr %x, align 4
  %eqtmp = icmp eq i32 %x3, 0
  %ifcond4 = icmp ne i1 %eqtmp, false
  br i1 %ifcond4, label %then5, label %else6

then5:                                            ; preds = %ifcont
  ret i32 0

else6:                                            ; preds = %ifcont
  br label %ifcont7

ifcont7:                                          ; preds = %else6
  %x8 = load i32, ptr %x, align 4
  %multmp = mul i32 %x8, 2
  ret i32 %multmp
}

define i32 @main() {
entry:
  %d = alloca i32, align 4
  %calltmp = call i32 @day_name(i32 3)
  store i32 %calltmp, ptr %d, align 4
  %sum = alloca i32, align 4
  %calltmp1 = call i32 @calc(i32 1, i32 10, i32 20)
  store i32 %calltmp1, ptr %sum, align 4
  %prod = alloca i32, align 4
  %calltmp2 = call i32 @calc(i32 3, i32 5, i32 6)
  store i32 %calltmp2, ptr %prod, align 4
  %p1 = alloca i32, align 4
  %calltmp3 = call i32 @piecewise(i32 -5)
  store i32 %calltmp3, ptr %p1, align 4
  %p2 = alloca i32, align 4
  %calltmp4 = call i32 @piecewise(i32 0)
  store i32 %calltmp4, ptr %p2, align 4
  %p3 = alloca i32, align 4
  %calltmp5 = call i32 @piecewise(i32 3)
  store i32 %calltmp5, ptr %p3, align 4
  %sum6 = load i32, ptr %sum, align 4
  %d7 = load i32, ptr %d, align 4
  %addtmp = add i32 %sum6, %d7
  ret i32 %addtmp
}
