using System.Security.Cryptography;

namespace SpyBrowser.Cursory.Internal;

/// <summary>NumPy PCG64 XSL-RR 128/64 bit generator; no mutable state is shared across calls.</summary>
internal sealed class Pcg64
{
    private const uint InitA=0x43b0d7e5, MultA=0x931e8875, InitB=0x8b51f9dd, MultB=0x58f38ded, MixL=0xca01f9dd, MixR=0x4973f715;
    private const double DoubleScale=1.0/9007199254740992.0;
    private static readonly UInt128 Multiplier=((UInt128)2549297995355413924UL<<64)|4865540595714422341UL;
    private UInt128 _state, _increment;
    private bool _spare; private uint _spareValue;

    internal Pcg64(UInt128? seed)
    {
        var seedValue=seed ?? RandomSeed(); var entropy=new List<uint>();
        do { entropy.Add((uint)seedValue); seedValue >>= 32; } while(seedValue != 0);
        var pool=new uint[4]; uint c=InitA;
        uint Hash(uint x) { var v=x^c; c=unchecked(c*MultA); v=unchecked(v*c); return v^(v>>16); }
        for(int i=0;i<4;i++) pool[i]=Hash(i<entropy.Count?entropy[i]:0);
        uint Mix(uint x,uint y) { var v=unchecked(MixL*x-MixR*y); return v^(v>>16); }
        for(int s=0;s<4;s++) for(int d=0;d<4;d++) if(s!=d) pool[d]=Mix(pool[d],Hash(pool[s]));
        for(int s=4;s<entropy.Count;s++) for(int d=0;d<4;d++) pool[d]=Mix(pool[d],Hash(entropy[s]));
        var words=new uint[8]; c=InitB;
        for(int i=0;i<8;i++){var v=pool[i%4]^c;c=unchecked(c*MultB);v=unchecked(v*c);words[i]=v^(v>>16);}
        ulong W(int i)=>words[i*2]|((ulong)words[i*2+1]<<32);
        var initial=((UInt128)W(0)<<64)|W(1); var sequence=((UInt128)W(2)<<64)|W(3);
        _increment=(sequence<<1)|1; Step(); _state+=initial; Step();
    }
    private static UInt128 RandomSeed(){Span<byte>b=stackalloc byte[16];RandomNumberGenerator.Fill(b);return new UInt128(BitConverter.ToUInt64(b[8..]),BitConverter.ToUInt64(b[..8]));}
    private void Step()=>_state=unchecked(_state*Multiplier+_increment);
    internal ulong NextUInt64(){Step();ulong v=(ulong)(_state>>64)^(ulong)_state;int r=(int)(_state>>122);return (v>>r)|(v<<((-r)&63));}
    internal uint NextUInt32(){if(_spare){_spare=false;return _spareValue;}var v=NextUInt64();_spare=true;_spareValue=(uint)(v>>32);return (uint)v;}
    internal double NextDouble()=> (NextUInt64()>>11)*DoubleScale;
    internal int Integers(int high){if(high<=0)throw new ArgumentOutOfRangeException(nameof(high));uint range=(uint)(high-1);if(range==0)return 0;if(range==uint.MaxValue)return (int)NextUInt32();ulong ex=(ulong)range+1;ulong scaled=(ulong)NextUInt32()*ex, low=(uint)scaled;ulong threshold=(uint.MaxValue-range)%ex;while(low<threshold){scaled=(ulong)NextUInt32()*ex;low=(uint)scaled;}return (int)(scaled>>32);}
}
