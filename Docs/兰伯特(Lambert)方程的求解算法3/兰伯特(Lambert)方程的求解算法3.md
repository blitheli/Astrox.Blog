# 兰伯特(Lambert)方程的求解算法3

在前2篇文章中，介绍了兰伯特方程的基本概念，并给出了无量纲飞行时间$T$的具体的算法，且给出了由时间$T$求解自变量$x$的具体算法。本章给出最终的算法：转移轨道两端点$p_1、p_2$的径向和横向速度。

## 速度V的求解（VLAMB）
输入：
1. $\mu$，中心天体的引力常数；
2. $r_1$，转移轨道起点$P_1$到引力中心C的距离 (m)；
3.  $r_2$，转移轨道起点$P_2$到引力中心C的距离(m) ；
4. $\theta$， 转移轨道的地心夹角；
5. $\Delta t$，转移轨道的飞行时间(s)；

输出： 

6. $n$,n=-1:非正常返回；n=0:无解；n=1:1个解；n=2:2个解
7. $V_{1r1}$,起点$p_1$处径向速度大小(第1个解)
8. $V_{1t1}$,起点$p_1$处切向速度大小(第1个解)
9. $V_{2r1}$,终点$p_2$处径向速度大小(第1个解)
10. $V_{2t1}$,终点$p_2$处切向速度大小(第1个解)
11. $V_{1r1}$,起点$p_1$处径向速度大小(第2个解)
12. $V_{1t1}$,起点$p_1$处切向速度大小(第2个解)
13. $V_{2r1}$,终点$p_2$处径向速度大小(第2个解)
14. $V_{2t1}$,终点$p_2$处切向速度大小(第2个解)

#### 参数处理
输入参数$\theta$为转移角度，允许大于$2\pi$，因此首先将其转换为$2\pi$以内。
$$m=INT(\frac{\theta}{2\pi})\\
\theta=\theta-m*2\pi$$ 
上式中$INT$为取整，由此可得到转移圈数$m$。
依据输入参数，计算下列变量：
$$ 
\begin{array}{l}
c=\sqrt{(r_1-r_2)^2+4r_1r_2sin^2(\theta/2)} \\ 
s=(r_1+r_2+c)/2 \\
q=\sqrt{r_1r_2}cos(\theta/2)/s \\
1-q^2=c/s \\
T_i=\sqrt{8\mu/s^3}\Delta t
\end{array}
$$
#### 调用XLAMB
有了$m,q,1-q^2,T_i$，则直接调用XLAM程序得到方程的根$x,x_+$，以及根的个数$n$。

当$m=0$时，$n=1$，仅$x$返回值有效；
当$m>0$时，通常$n=2$（也有可能为0或1）,则$x,x_+$返回值皆有效。
#### 速度公式
有了$x$，则可得到起点、终点的径向和切向速度大小公式：
$$ 
\begin{array}{l}
V_{1r1}=\gamma((qz-x)-\rho(qz+x))/r_1 \\ 
V_{2r1}=-\gamma((qz-x)+\rho(qz+x))/r_2 \\ 
V_{1t1}=\gamma\sigma(z+qx)/r_1 \\ 
V_{2t1}=\gamma\sigma(z+qx)/r_2 
\tag1
\end{array}
$$
上式中：
$$ 
\begin{array}{l}
\gamma =\sqrt{\mu s/2} \\ 
\rho =(r_1-r_2)/c \\ 
\sigma =2\sqrt{\frac{r_1r_2}{c^2}}sin(\frac{\theta}{2})
\end{array}
$$
注意，上式中，若$c=0$时，$\rho=0,\sigma=1$
若有两个解，则将$x_+$带入式(1)，即可得到第2个解$V_{1r2}、V_{2r2}、V_{1t2}、V_{2t2}$。
#### $qz-x、qz+x、z+qx$的求解
式(1)中，涉及到$qz-x、qz+x、z+qx$的计算，在算法中，是通过TLAMB求解的，即：
$$T,T',T'',T'''=T(m,q,1-q^2,x,n=-1)$$
则返回值中
$$
T'=qz-x \\
T''=qz+x \\
T'''=z+qx
$$
TLAMB内部计算时的逻辑如下：
1. $qx==0$时
$$
T'=qz-x \\
T''=qz+x \\
T'''=z+qx
$$
2. $qx<0$时
$$
\begin{array}{l}
T'=qz-x \\
T''=(1-q^2)(q^2u-x^2)/(qz-x) \\
T'''=(1-q^2)/(z-qx)
\end{array}
$$
3. $qx>0$时
$$
\begin{array}{l}
T'=(1-q^2)(q^2u-x^2)/(qz+x) \\
T''=qz+x \\
T'''=z+qx
\end{array}
$$
实际上，上面后两种情形本质都和第1种情形（$qx==0$）相同，只不过为了提高计算的精度改变一下公式形式而已。

## C#源码

```csharp
/// <summary>
/// Lambert 方程的径向,横向速度解 (R.H.Gooding 方法)
/// <para>已知始末状态的几何构型(r1,r2,th,tdelt)，求相应的速度(V1,V2)</para>
/// <para>调用的子程序: TLamb,XLamb</para>
/// <para>算法引用的文献为：</para>
/// <para>Gooding,R.H.:: 1988a,'On the Solution of Lambert's Orbital Boundary-Value Problem',RAE Technical Report 88027</para>
/// <para>输入输出参数最好都无量纲化，否则和Gm一样都采用相同的单位体系</para>
/// <para>调用前最好检查输入参数(u>0,r1>0,r2>0,th>0,tdelt>0)</para>
/// </summary>
/// <param name="Gm">引力常数</param>
/// <param name="r1">起点位置地心距(与Gm单位一致)</param>
/// <param name="r2">终点位置地心距(与Gm单位一致)</param>
/// <param name="th">转移角度rad;( >=0皆可)</param>
/// <param name="tdelt">转移时间(与Gm单位一致)</param>
/// <param name="n">解的个数(-1,,0,1,2)</param>
/// <param name="vr11">解1的r1径向速度</param>
/// <param name="vt11">解1的r1切向速度</param>
/// <param name="vr12">解1的r2径向速度</param>
/// <param name="vt12">解1的r2切向速度</param>
/// <param name="vr21">解2的r1径向速度</param>
/// <param name="vt21">解2的r1切向速度</param>
/// <param name="vr22">解2的r2径向速度</param>
/// <param name="vt22">解2的r2切向速度</param>
public static void VLAMB(double Gm, double r1, double r2, double th, double tdelt, out int n, out double vr11, out double vt11, out double vr12, out double vt12, out double vr21, out double vt21, out double vr22, out double vt22)
{
    vr11 = vt11 = vr12 = vt12 = vr21 = vt21 = vr22 = vt22 = 0.0;
    double unused, x;

    //  转移圈数   
    int m = (int)(th / 2.0 / Math.PI);
    double thr2 = th / 2.0 - m * Math.PI;

    double dr = r1 - r2;
    double r1r2 = r1 * r2;
    double r1r2th = 4.0 * r1r2 * Math.Sin(thr2) * Math.Sin(thr2);
    double csq = dr * dr + r1r2th;
    double c = Math.Sqrt(csq);
    double s = (r1 + r2 + c) / 2.0;
    double gms = Math.Sqrt(Gm * s / 2.0);    //gamma
    double qsqfm1 = c / s;
    double q = Math.Sqrt(r1r2) * Math.Cos(thr2) / s;

    double rho = 0.0;
    double sig = 1.0;		//σ^2
    if (c != 0.0)
    {
        rho = dr / c;
        sig = r1r2th / csq;
    }

    //  无量纲时间t=sqrt(8u/s^3)*Δt  
    double t = 4.0 * gms * tdelt / (s * s);

    //  调用XLamb,求解最后x,n    
    double x1, x2;
    XLAMB(m, q, qsqfm1, t, out n, out x1, out x2);
    if ((m == 0) && (n < 1)) throw new Exception("Lambert方程求解出错！");

    //  计算径向和切向的速度大小              
    for (int i = 1; i <= n; i++)
    {
        if (i == 1)
            x = x1;                
        else                
            x = x2;

        //  从TLAMB中计算:qz-x/qz+x/qx+z
        double qzminx, qzplx, zplqx;
        TLAMB(m, q, qsqfm1, x, -1, out unused, out qzminx, out qzplx, out zplqx);

        double vt2 = gms * zplqx * Math.Sqrt(sig);
        double vr1 = gms * (qzminx - qzplx * rho) / r1;
        double vt1 = vt2 / r1;
        double vr2 = -gms * (qzminx + qzplx * rho) / r2;
        vt2 = vt2 / r2;

        if (i == 1)     //第1个解
        {
            vr11 = vr1;
            vt11 = vt1;
            vr12 = vr2;
            vt12 = vt2;
        }
        else            //第2个解
        {
            vr21 = vr1;
            vt21 = vt1;
            vr22 = vr2;
            vt22 = vt2;
        }
    }
}


```



