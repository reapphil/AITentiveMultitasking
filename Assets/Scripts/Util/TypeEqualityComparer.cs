using System;
using System.Collections.Generic;

public class TypeEqualityComparer : IEqualityComparer<Type>
{
    public bool Equals(Type x, Type y)
    {
        // Compare the underlying types
        return x.GetHashCode() == y.GetHashCode();
    }

    public int GetHashCode(Type obj)
    {
        // Use the hash code of the underlying type
        return obj?.GetHashCode() ?? 0;
    }
}