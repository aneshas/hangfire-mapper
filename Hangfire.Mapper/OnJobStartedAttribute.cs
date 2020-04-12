using System;

namespace Hangfire.Mapper
{
    public class OnJobStartedAttribute : JobDisplayNameAttribute 
    {
        public OnJobStartedAttribute(string displayName) : base(displayName)
        {
        }
    }
}