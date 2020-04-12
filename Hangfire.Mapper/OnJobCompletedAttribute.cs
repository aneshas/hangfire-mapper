using System;

namespace Hangfire.Mapper
{
    public class OnJobCompletedAttribute : JobDisplayNameAttribute 
    {
        public OnJobCompletedAttribute(string displayName) : base(displayName)
        {
        }
    }
}