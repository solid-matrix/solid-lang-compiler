; ModuleID = 'solid_module'
source_filename = "solid_module"
target triple = "x86_64-pc-windows-msvc"

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

define i32 @main() {
entry:
  %a = alloca i32, align 4
  store i32 10, ptr %a, align 4
  %b = alloca i32, align 4
  store i32 5, ptr %b, align 4
  %a1 = load i32, ptr %a, align 4
  %b2 = load i32, ptr %b, align 4
  %calltmp = call i32 @max(i32 %a1, i32 %b2)
  ret i32 %calltmp
}
