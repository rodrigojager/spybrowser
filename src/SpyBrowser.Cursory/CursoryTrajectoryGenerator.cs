using SpyBrowser.Cursory.Internal;

namespace SpyBrowser.Cursory;

/// <summary>Standalone experimental Cursory trajectory generator. This preview does not integrate with Playwright.</summary>
public static class CursoryTrajectoryGenerator
{
    private const double Eps=2.220446049250313e-16;
    /// <summary>Generate a recorded, selected and transformed path between two finite points.</summary>
    public static Trajectory Generate(TrajectoryPoint start, TrajectoryPoint end, TrajectoryOptions? options=null)
    {
        options ??= new();
        if(!double.IsFinite(start.X)||!double.IsFinite(start.Y)||!double.IsFinite(end.X)||!double.IsFinite(end.Y))throw new ArgumentOutOfRangeException(nameof(start),"Coordinates must be finite.");
        if(!double.IsFinite(options.Frequency)||options.Frequency<=0||options.Frequency>10000)throw new ArgumentOutOfRangeException(nameof(options.Frequency));
        if(!double.IsFinite(options.FrequencyRandomizer)||options.FrequencyRandomizer<0||options.FrequencyRandomizer>10000)throw new ArgumentOutOfRangeException(nameof(options.FrequencyRandomizer));
        if(!double.IsFinite(options.Directness)||options.Directness<0||options.Directness>1)throw new ArgumentOutOfRangeException(nameof(options.Directness));
        if(start==end)return new([start],[0]);
        var rng=new RandomSampler(new Pcg64(options.Seed));var data=Dataset.Shared.Value;
        double dx=end.X-start.X,dy=end.Y-start.Y,length=Hypot(dx,dy);
        if(!double.IsFinite(dx)||!double.IsFinite(dy)||!double.IsFinite(length))throw new ArgumentOutOfRangeException(nameof(end),"Coordinate displacement exceeds the supported finite range.");
        var chosen=Select(data,start,end,length,options.Directness,rng);
        var rec=chosen.Record;
        var times=(double[])rec.Timing.Clone();
        if(length>0&&length<rec.Length){double scale=Math.Sqrt(length/rec.Length),t0=times[0];for(int i=0;i<times.Length;i++)times[i]=t0+(times[i]-t0)*scale;}
        var path=Jitter(rec.Points,length,rng);
        path=Knot(path,start,end,rng);
        path=Morph(path,start,end,dx,dy,length);
        double tStart=times[0];for(int i=0;i<times.Length;i++)times[i]-=tStart;
        var sampleTimes=SampleTimes(times,options.Frequency,options.FrequencyRandomizer,rng);
        var sampled=new TrajectoryPoint[sampleTimes.Length];int prior=0;
        for(int j=0;j<sampled.Length;j++){double t=sampleTimes[j];while(prior+1<times.Length&&times[prior+1]<=t)prior++;int next=Math.Min(prior+1,times.Length-1);double a=times[next]!=times[prior]?(t-times[prior])/(times[next]-times[prior]):0;sampled[j]=new(path[prior].X+a*(path[next].X-path[prior].X),path[prior].Y+a*(path[next].Y-path[prior].Y));}
        sampled=Knot(sampled,start,end,rng);sampled=Jitter(sampled,length,rng);sampled=Morph(sampled,start,end,dx,dy,length);
        if(sampled.Any(p=>!double.IsFinite(p.X)||!double.IsFinite(p.Y)))throw new InvalidDataException("Cursory produced a non-finite coordinate.");
        return new(sampled,sampleTimes);
    }
    private static (Recording Record,int Index) Select(Recording[] d,TrajectoryPoint a,TrajectoryPoint b,double length,double directness,RandomSampler rng)
    {
        var candidates=new List<int>();
        void Nearest(TrajectoryPoint target,int n){double dx=target.X-a.X,dy=target.Y-a.Y,l=Hypot(dx,dy);var scores=new (double score,int i)[d.Length];for(int i=0;i<d.Length;i++){var r=d[i];double rx=r.Points[^1].X-r.Points[0].X,ry=r.Points[^1].Y-r.Points[0].Y,rl=Hypot(rx,ry);double score=l==0?r.Length:(0.8*(1-(rl==0?0:(rx*dx+ry*dy)/(rl*l)))+0.2*Math.Abs(rl-l)/Math.Max(l,1));scores[i]=(score,i);}Array.Sort(scores,(x,y)=>{int c=x.score.CompareTo(y.score);return c!=0?c:x.i.CompareTo(y.i);});for(int k=0;k<n;k++)candidates.Add(scores[k].i);}
        Nearest(b,5);double jitter=length*.1;for(int k=0;k<20;k++)Nearest(new(b.X+rng.Uniform(-jitter,jitter),b.Y+rng.Uniform(-jitter,jitter)),5);
        var efficiencies=d.Select(r=>{var x=r.Points[^1].X-r.Points[0].X;var y=r.Points[^1].Y-r.Points[0].Y;return Hypot(x,y)/Math.Max(r.Length,Eps);}).Order().ToArray();double pref=.2+.75*directness;var weights=new double[candidates.Count];for(int i=0;i<weights.Length;i++){int rank=Array.BinarySearch(efficiencies,Eff(d[candidates[i]]));if(rank<0)rank=~rank;else while(rank<efficiencies.Length&&efficiencies[rank]<=Eff(d[candidates[i]]))rank++;double efficiencyRank=rank/(double)d.Length;double off=(efficiencyRank-pref)/.2,center=2*efficiencyRank-1;weights[i]=Math.Exp(-.5*off*off)+.05*(1+.1*Math.Pow(Math.Abs(center),3));}int selected=candidates[rng.Choice(weights)];return(d[selected],selected);
    }
    private static double Eff(Recording r)=>Hypot(r.Points[^1].X-r.Points[0].X,r.Points[^1].Y-r.Points[0].Y)/Math.Max(r.Length,Eps);
    private static TrajectoryPoint[] Morph(TrajectoryPoint[] p,TrajectoryPoint a,TrajectoryPoint b,double dx,double dy,double len){var f=p[0];var l=p[^1];double ox=l.X-f.X,oy=l.Y-f.Y,ol=Math.Sqrt(ox*ox+oy*oy),scale=ol!=0?len/ol:1,rot=Math.Atan2(dy,dx)-Math.Atan2(oy,ox),c=Math.Cos(rot),s=Math.Sin(rot);var q=p.Select(v=>{double x=(v.X-f.X)*scale,y=(v.Y-f.Y)*scale;return new TrajectoryPoint(x*c-y*s+a.X,x*s+y*c+a.Y);}).ToArray();q[0]=a;q[^1]=b;return q;}
    private static TrajectoryPoint[] Jitter(TrajectoryPoint[] p,double len,RandomSampler rng){int n=p.Length;double[] gaps=new double[n],nx=new double[n],ny=new double[n],sc=new double[n],noise=new double[n];bool ints=p.All(v=>v.X==Math.Truncate(v.X)&&v.Y==Math.Truncate(v.Y));for(int i=0;i<n-1;i++)gaps[i]=Math.Sqrt(Math.Pow(p[i+1].X-p[i].X,2)+Math.Pow(p[i+1].Y-p[i].Y,2));for(int i=0;i<n;i++){double avg=((i>0?gaps[i-1]:0)+(i<n-1?gaps[i]:0))/2;sc[i]=.01*(avg/Math.Max(avg,1))*Math.Min(1,len/400);var ahead=p[i==n-1?n-1:i+1];var behind=p[i==0?0:i-1];double tx=ahead.X-behind.X,ty=ahead.Y-behind.Y,tl=Math.Sqrt(tx*tx+ty*ty);if(tl>Eps){nx[i]=-ty/tl;ny[i]=tx/tl;if(ints){nx[i]=Math.Truncate(nx[i]);ny[i]=Math.Truncate(ny[i]);}}noise[i]=rng.StandardNormal();}var q=new TrajectoryPoint[n];double corr=.75,fac=Math.Sqrt(1-corr*corr),offset=0;for(int i=0;i<n;i++){offset=i==0?noise[i]:corr*offset+fac*noise[i];q[i]=new(p[i].X+nx[i]*offset*sc[i],p[i].Y+ny[i]*offset*sc[i]);}return q;}
    private static TrajectoryPoint[] Knot(TrajectoryPoint[] p,TrajectoryPoint a,TrajectoryPoint b,RandomSampler rng){int n=p.Length;double[] ox=new double[n],oy=new double[n];for(int k=0;k<5;k++){double x=rng.Normal((Math.Max(a.X,b.X)-Math.Min(a.X,b.X))/4)+(a.X+b.X)/2,y=rng.Normal((Math.Max(a.Y,b.Y)-Math.Min(a.Y,b.Y))/4)+(a.Y+b.Y)/2;x=Math.Clamp(x,Math.Min(a.X,b.X),Math.Max(a.X,b.X));y=Math.Clamp(y,Math.Min(a.Y,b.Y),Math.Max(a.Y,b.Y));var tx=new double[n];var ty=new double[n];var dist=new double[n];double max=0;for(int i=0;i<n;i++){tx[i]=x-p[i].X;ty[i]=y-p[i].Y;dist[i]=Math.Sqrt(tx[i]*tx[i]+ty[i]*ty[i]);max=Math.Max(max,dist[i]);}if(max<1e-6)continue;for(int i=0;i<n;i++){double f=(1-dist[i]/max)*.15;ox[i]+=tx[i]*f;oy[i]+=ty[i]*f;}}return p.Select((v,i)=>new TrajectoryPoint(v.X+Math.CopySign(Math.Sqrt(Math.Abs(ox[i])),ox[i]),v.Y+Math.CopySign(Math.Sqrt(Math.Abs(oy[i])),oy[i]))).ToArray();}
    private static double[] SampleTimes(double[] t,double frequency,double randomizer,RandomSampler rng){double total=Math.Max(1,Rint(t[^1]-t[0]));double requestedCount=Rint(total*frequency/1000);if(!double.IsFinite(requestedCount)||requestedCount>100_000)throw new ArgumentOutOfRangeException(nameof(frequency),"Requested sample count exceeds 100000.");int count=Math.Max(1,(int)requestedCount);var intervals=t.Zip(t.Skip(1),(x,y)=>y-x).Where(x=>x>0).ToArray();if(intervals.Length==0)intervals=[1];int roll=rng.Integer(intervals.Length);var a=new double[count];for(int i=0;i<count;i++)a[i]=intervals[((int)Math.Floor((i+.5)*intervals.Length/count)+roll)%intervals.Length];Rescale(a,total);if(randomizer!=0){for(int i=0;i<count;i++)a[i]=Math.Max(a[i]+rng.Uniform(-randomizer,randomizer),Eps);Rescale(a,total);}var result=new double[count+1];double elapsed=0;for(int i=0;i<count;i++){elapsed+=a[i];result[i+1]=Rint(elapsed);}result[^1]=total;return result;}
    private static void Rescale(double[] x,double total){double sum=NumericCompat.PairwiseSum(x);for(int i=0;i<x.Length;i++)x[i]*=total/sum;}
    private static double Rint(double v)=>Math.Round(v,MidpointRounding.ToEven);
    private static double Hypot(double x,double y)=>NumericCompat.Hypot(x,y);
}
