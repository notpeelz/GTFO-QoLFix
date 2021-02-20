using System;
using UnhollowerBaseLib;
using UnhollowerBaseLib.Runtime;

namespace QoLFix
{
    public static class UnhollowerExtensions
    {
        public static bool Is<T>(this Il2CppObjectBase obj)
        {
            var nestedTypeClassPointer = Il2CppClassPointerStore<T>.NativeClassPtr;
            if (nestedTypeClassPointer == IntPtr.Zero)
                throw new ArgumentException($"{typeof(T)} is not an Il2Cpp reference type");

            var ownClass = IL2CPP.il2cpp_object_get_class(obj.Pointer);
            if (!IL2CPP.il2cpp_class_is_assignable_from(nestedTypeClassPointer, ownClass))
                return false;

            if (RuntimeSpecificsStore.IsInjected(ownClass))
                return ClassInjectorBase.GetMonoObjectFromIl2CppPointer(obj.Pointer) is T;

            return true;
        }
    }
}
