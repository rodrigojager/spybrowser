namespace SpyBrowser.Cursory.Internal;
internal sealed class RandomSampler(Pcg64 bit)
{
 internal double Uniform(double low,double high)=>low+(high-low)*bit.NextDouble();
 internal double Normal(double scale)=>scale*StandardNormal();
 internal int Integer(int high)=>bit.Integers(high);
 internal int Choice(double[] weights){var cumulative=new double[weights.Length];double total=0;for(int i=0;i<weights.Length;i++){total+=weights[i];cumulative[i]=total;}for(int i=0;i<cumulative.Length;i++)cumulative[i]/=total;double d=bit.NextDouble();int lo=0,hi=cumulative.Length;while(lo<hi){int m=(lo+hi)/2;if(d<cumulative[m])hi=m;else lo=m+1;}return lo;}
 internal double StandardNormal(){while(true){ulong draw=bit.NextUInt64();int index=(int)(draw&255);draw>>=8;bool negative=(draw&1)!=0;ulong magnitude=(draw>>1)&0x000fffffffffffffUL;double x=magnitude*ZigguratTables.WI_DOUBLE[index];if(negative)x=-x;if(magnitude<ZigguratTables.KI_DOUBLE[index])return x;if(index==0){while(true){double xx=-ZigguratTables.ZIGGURAT_NOR_INV_R*Math.Log(1-bit.NextDouble());double yy=-Math.Log(1-bit.NextDouble());if(yy+yy>xx*xx)return ((magnitude>>8)&1)!=0?-(ZigguratTables.ZIGGURAT_NOR_R+xx):ZigguratTables.ZIGGURAT_NOR_R+xx;}}double wedge=(ZigguratTables.FI_DOUBLE[index-1]-ZigguratTables.FI_DOUBLE[index])*bit.NextDouble()+ZigguratTables.FI_DOUBLE[index];if(wedge<Math.Exp(-0.5*x*x))return x;}}
}
