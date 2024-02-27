
using Superintendent.Core.CommandSink;
using System;

/*
 *
 *
 *
 *
 *   This file is auto generated
 *     Do not modify manually
 *        Edit the template
 *
 *
 *
 *
 *
 */

namespace Superintendent.Core
{
    public abstract class CommandSinkClient<TOffsets>
    {
        public readonly ICommandSink CommandSink;
        public readonly TOffsets Offsets;
        public IAllocator? Allocator { get; set; }

        protected CommandSinkClient(ICommandSink sink, TOffsets offsets, IAllocator? allocator = null)
        {
            this.CommandSink = sink;
            this.Offsets = offsets;
            this.Allocator = allocator;
        }
    }

    public class InvocationException : Exception { }

    public interface IFun<T> where T: Fun
    {
        static abstract T Create(nint address);
    }

    public class Fun : IFun<Fun>
    {
        public static Fun Create(nint address) => new(address);

        public Fun(nint address)
        {
            this.Address = address;
        }

        public nint Address { get; }

        public static implicit operator Fun(nint address) => new(address);
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class ParamNamesAttribute : Attribute
    {
        private readonly string[] names;

        public ParamNamesAttribute(params string[] names)
        {
            this.names = names;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class SpanPointerAttribute : Attribute
    {
        public SpanPointerAttribute()
        {
        }
    }

    ///<summary>Used in Fun<T> args to indicate how to encode the string. Generated function will accept System.String</summary>
    public class Utf8String { }

    ///<summary>Used in Fun<T> args to indicate how to encode the string. Generated function will accept System.String</summary>
    public class AsciiString { }

    ///<summary>Used in Fun<T> args to indicate how to encode the string. Generated function will accept System.String</summary>
    public class Utf16String { }

    public class Fun<TRet> : Fun, IFun<Fun<TRet>>
    {
        public static Fun<TRet> Create(nint address) => new(address);
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TRet> : Fun, IFun<Fun<TArg1, TRet>>
    {
        public static Fun<TArg1, TRet> Create(nint address) => new(address);
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TArg2, TRet> : Fun, IFun<Fun<TArg1, TArg2, TRet>>
    {
        public static Fun<TArg1, TArg2, TRet> Create(nint address) => new(address);
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TArg2, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TArg2, TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TArg2, TArg3, TRet> : Fun, IFun<Fun<TArg1, TArg2, TArg3, TRet>>
    {
        public static Fun<TArg1, TArg2, TArg3, TRet> Create(nint address) => new(address);
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TArg2, TArg3, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TArg2, TArg3, TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TArg2, TArg3, TArg4, TRet> : Fun, IFun<Fun<TArg1, TArg2, TArg3, TArg4, TRet>>
    {
        public static Fun<TArg1, TArg2, TArg3, TArg4, TRet> Create(nint address) => new(address);
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TArg2, TArg3, TArg4, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TArg2, TArg3, TArg4, TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TRet> : Fun, IFun<Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TRet>>
    {
        public static Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TRet> Create(nint address) => new(address);
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TRet> : Fun, IFun<Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TRet>>
    {
        public static Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TRet> Create(nint address) => new(address);
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TRet> fun) => fun.Address;
    }
    public class Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TRet> : Fun, IFun<Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TRet>>
    {
        public static Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TRet> Create(nint address) => new(address);
        public Fun(nint address) : base(address) { } 
        public static implicit operator Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TRet>(nint address) => new (address); 
        public static implicit operator nint(Fun<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7, TRet> fun) => fun.Address;
    }

    public class FunVoid : Fun, IFun<FunVoid>
    {
        public static FunVoid Create(nint address) => new(address);
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid(nint address) => new (address); 
        public static implicit operator nint(FunVoid fun) => fun.Address;
    }

    public class FunVoid<TArg1> : Fun, IFun<FunVoid<TArg1>>
    {
        public static FunVoid<TArg1> Create(nint address) => new(address);
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1> fun) => fun.Address;
    }
    public class FunVoid<TArg1, TArg2> : Fun, IFun<FunVoid<TArg1, TArg2>>
    {
        public static FunVoid<TArg1, TArg2> Create(nint address) => new(address);
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1, TArg2>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1, TArg2> fun) => fun.Address;
    }
    public class FunVoid<TArg1, TArg2, TArg3> : Fun, IFun<FunVoid<TArg1, TArg2, TArg3>>
    {
        public static FunVoid<TArg1, TArg2, TArg3> Create(nint address) => new(address);
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1, TArg2, TArg3>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1, TArg2, TArg3> fun) => fun.Address;
    }
    public class FunVoid<TArg1, TArg2, TArg3, TArg4> : Fun, IFun<FunVoid<TArg1, TArg2, TArg3, TArg4>>
    {
        public static FunVoid<TArg1, TArg2, TArg3, TArg4> Create(nint address) => new(address);
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1, TArg2, TArg3, TArg4>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1, TArg2, TArg3, TArg4> fun) => fun.Address;
    }
    public class FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5> : Fun, IFun<FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5>>
    {
        public static FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5> Create(nint address) => new(address);
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5> fun) => fun.Address;
    }
    public class FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> : Fun, IFun<FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>>
    {
        public static FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> Create(nint address) => new(address);
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6> fun) => fun.Address;
    }
    public class FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> : Fun, IFun<FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>>
    {
        public static FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> Create(nint address) => new(address);
        public FunVoid(nint address) : base(address) { } 
        public static implicit operator FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7>(nint address) => new (address); 
        public static implicit operator nint(FunVoid<TArg1, TArg2, TArg3, TArg4, TArg5, TArg6, TArg7> fun) => fun.Address;
    }
    public interface IPtr<T> where T: IPtr<T>
    {
        static abstract T Create(nint baseOffset, nint[]? chain);
        nint Base { get; set; }
        nint[] Chain { get; set; }
    }

    ///<summary>An absolute pointer to a value.</summary>
    public struct AbsolutePtr<T> : IPtr<AbsolutePtr<T>> where T:  unmanaged
    {
        public nint Base { get; set; }
        public nint[] Chain { get; set; }

        public AbsolutePtr(nint baseOffset, nint[]? chain)
        {
            this.Base = baseOffset;
            this.Chain = chain ?? Array.Empty<nint>();
        }

        public Ptr<T> AsPtr() => new Ptr<T>(this.Base, this.Chain);

        public static AbsolutePtr<T> Create(nint baseOffset, nint[]? chain) => new AbsolutePtr<T>(baseOffset, chain);
    }
    
    ///<summary>A relative (to a module offset) pointer to a value.</summary>
    public struct Ptr<T> : IPtr<Ptr<T>> where T : unmanaged
    {
        public nint Base { get; set; }
        public nint[] Chain { get; set; }

        public Ptr(nint baseOffset, nint[]? chain)
        {
            this.Base = baseOffset;
            this.Chain = chain ?? Array.Empty<nint>();
        }

         public static Ptr<T> Create(nint baseOffset, nint[]? chain) => new Ptr<T>(baseOffset, chain);
    }
}
