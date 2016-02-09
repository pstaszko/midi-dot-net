using System;

namespace Midi.Common
{
    /// <summary>
    ///     Unmanaged helper class
    /// </summary>
    public static class Unmanaged
    {
        /// <summary>
        ///     Converts the UIntPtr to a IntPtr
        /// </summary>
        /// <param name="ptr">Pointer to convert</param>
        /// <returns></returns>
        public static IntPtr ConvertToIntPtr(UIntPtr ptr)
        {
            //http://stackoverflow.com/questions/3762113/how-can-an-uintptr-object-be-converted-to-intptr-in-c

            return unchecked((IntPtr) (long) (ulong) ptr);
        }

        /// <summary>
        ///     Converts the UIntPtr to a IntPtr
        /// </summary>
        /// <param name="ptr">Pointer to convert</param>
        /// <returns></returns>
        public static IntPtr ToIntPtr(this UIntPtr ptr)
        {
            return ConvertToIntPtr(ptr);
        }
    }
}