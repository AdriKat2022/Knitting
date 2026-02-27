using System;

namespace Knitting.Attributes
{
    /// <summary>
    /// Marks this function to run just after the transmutation for this object is complete.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class PostTransmuteAttribute : Attribute
    {
        
    }
}