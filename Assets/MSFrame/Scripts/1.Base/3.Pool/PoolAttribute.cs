using System;

namespace MSFrame
{

[AttributeUsage(AttributeTargets.Class,AllowMultiple =false)]
public class PoolAttribute : Attribute
{
    public int maxNum;
}
}
