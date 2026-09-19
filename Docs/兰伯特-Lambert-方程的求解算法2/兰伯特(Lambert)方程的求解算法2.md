# 兰伯特(Lambert)方程的求解算法2

在前一文章中，介绍了兰伯特方程的基本概念，并给出了无量纲飞行时间$T$的具体的算法，本节给出上述逆过程，即由无量纲飞行时间$T$求解自变量$x$。

已知$T$求解自变量$x$采用函数求根方法，即求解下式的根：
$$T(m,q,x)-T_i=0
$$

此处采用Halley迭代法(需要使用到$T$对$x$的2阶导数）。
## 自变量X的求解（XLAMB）
输入：
1. $m$
2. $q$
3. $1-q^2$
4. $T_i$,无量纲飞行时间

输出：
5. $n$,n=-1:非正常返回；n=0:无解；n=1:1个解；n=2:2个解
6. $x$,第1个解
7. $x_+$,第2个解（n=2时）

### 初值设定$x_0$
为了使程序计算中迭代算法的高效快速，合适的初值选取是非常重要的。
首先给出本小节公式中一些常数值：
$$
\begin{cases}
c_0=1.7 \\
c_1=0.5 \\
c_2=0.03 \\
c_3=0.15 \\
c_{41}=1 \\
c_{42} = 0.24
\end{cases}
$$
转移角度$\theta$
$$\theta/2 =arctan2(1-q^2,2q)$$
根据转移圈数$m$的不同，$x_0$的初值也不同，下面分情况讨论。
#### $m=0$时
此时对应转移角度不超过360°情形，由$T$的图像可知，$x$必有解，且只有一个解。

因此，输出参数$n=1$

首先计算$x=0$时的无量纲时间$T_0$,即：$$T_0=T(q,m=0,x=0)$$
1. $T_i\le T_0$
$$x_0=-\frac{T_0(T_i-T_0)}{4T_i}$$
2. $T_i>T_0$ 
令
$$
x_{01}=-\frac{T_i-T_0}{T_i-T_0+4} \\
x_{02}=-\sqrt{\frac{T_i-T_0}{T_i+T_0/2}} \\
W=x_{01}+c_0\sqrt{2-\theta/\pi} 
$$
有
$$
x_{03} =
\begin{cases}
x_{01} & \text{if $W\ge0$} \\
x_{01}+(-W)^{1/16}(x_{02}-x_{01}）& \text{if $W<0$}
\end{cases}
$$
则有：
$$x_0=\lambda x_{03}$$
其中：
$$\lambda = 1+c_1x_{03}(1+x_{01})-c_2x^2_{03}\sqrt{1+x_{01}}$$
#### $m>0$时
此时对应多圈转移情形，由$T$的图像可知，此时$x$的范围为$(-1,1)$，即为椭圆轨道。且$m$值不同，无量纲时间$T$有最小值。当给定$T_i$时，有可能无解。
#####  最小时间$T_m$及$x_m$的求解
首先求解出最小时间$T_m$及其自变量$x_m$，也即寻找方程$T'(q,m,x)=0$ 的根 ，其迭代过程采用Halley方法的迭代公式，因此在迭代求解过程中需要求解$T$的三阶导数。

迭代时，仍需要一个合适的初值$x_m$，其由下式给出：
$$
x_m=
\begin{cases}
x_{m,\pi} =\frac{4}{3\pi(2m+1)} & \text{if $\theta =\pi$} \\
x_{m,\pi}(\frac{\theta}{\pi})^{1/8} & \text{if $\theta <\pi$} \\
x_{m,\pi}(2-(2-\frac{\theta}{\pi})^{1/8}) & \text{if $\theta >\pi$} 
\end{cases}
$$
有了初值后，采用halley迭代法求解方程$T'(q,m,x)=0$ 的根。
迭代公式为：
$$
T_m,T'_m,T''_m,T'''_m=T(m,q,1-q^2,x_m,3) \\
x_m=x_m-\frac{T'T''}{T''T''-T'T'''/2}
$$
每一步迭代更新$x_m$前保留其值为$x'_m=x_m$.
限定最大迭代次数为12次，迭代过程中，若$T''==0$，则跳出循环；若$|x'_m/x_m-1|<3×10^{-7}$也跳出循环。
若迭代次数超过12次，则令$n=-1$，程序退出。
迭代完成后即可得到：$T_m,x_m,T''_m$，注意，若最终求得的$T''_m=0$，则令$T''_m=6m\pi$。

得到$T_m$后，则有：
 - 若$T_i<T_m$，令$n=0$，程序退出； 
 - 若$T_i=T_m$，令$x=x_m,n=1$，程序退出；
 - 若$T_i>T_m$，令$n=2$，有两解。由下面给出具体初值$x_{0+}、x_{0-}$。

##### $x>x_m$时
令
$$
x_{01}=\sqrt{\frac{T_i-T_m}{\frac{T''_m}{2}+\frac{T_i-T_m}{(1-x_m)^2}}} \\ 
$$
则初值$x_{0+}$为：
$$x_{0+}=x_m+\lambda x_{01}$$
上式中，
$$
\begin{cases}
W=\frac{4(x_m+x_{01})}{4+T-T_m}+(1-x_m-x_{01})^2 \\
\lambda'=(1+m+c_{41}(\theta/2\pi-0.5))/(1+c_3m）\\
\lambda=1-x_{01}\lambda'(c_1W+c_2x_{01}\sqrt{W})
\end{cases}
$$
##### $x<x_m$时
首先计算$x=0$时的无量纲时间$T_0$,即：$$T_0=T(q,m,x=0)$$
1. $T_i\le T_0$时
$$
x_{0-}=x_m-\sqrt{\frac{T_i-T_m}{T''_m/2-(T_i-T_m)(\frac{T''_m}{2(T_0-T_m)}-\frac{1}{x_m^2})}}
$$
2. $T_i> T_0$时
令
$$
x_{01}=-\frac{T_i-T_0}{T_i-T_0+4} \\
x_{02}=-\sqrt{\frac{T_i-T_0}{T_i+T_0/2}} \\
W=x_{01}+c_0\sqrt{2-\theta/\pi} 
$$
有
$$
x_{03} =
\begin{cases}
x_{01} & \text{if $W\ge0$} \\
x_{01}+(-W)^{1/16}(x_{02}-x_{01}）& \text{if $W<0$}
\end{cases}
$$
则有：
$$x_{0-}=\lambda x_{03}$$
其中：
$$
\begin{cases}
\lambda = 1+c_1x_{03}\lambda'(1+x_{01})-c_2x^2_{03}\lambda'\sqrt{1+x_{01}} \\
\lambda'=(1+m+c_{42}(\theta/2\pi-0.5))/(1+c_3m）
\end{cases}
$$
### 迭代求根
有了初值后，采用Halley迭代法。迭代公式为：
$$
T,T',T''=T(m,q,1-q^2,x_0,2) \\
x_0=x_0+\frac{(T_i-T)T'}{T'T'+(T_i-T)T''/2}
$$
迭代次数设定为3次即可。
迭代完成后，令$x=x_0$，程序退出；
若$m>0$，即有两解的情况下，分别将初值$x_{0+}、x_{0-}$带入上述迭代式，最终得到的解分别赋值给$x、x_+$

### C#源码
```csharp
  //  Lambert 无量纲方程x的求解 (R.H.Gooding 方法)
//--------------------------------------------------------------------
//   1   已知tin,寻找下列方程的根x
//       tin=sqrt(8u/s^3)*Δt= 2*pi*m/(1-x*x)^1.5+
//           4/3*(F[3,1;2.5;0.5*(1-x)]-q^3*F[3,1;2.5;0.5*(1-y)])
//       其中:   y=sqrt(1-q*q+q^2*x^2)=sqrt(qsqfm1+q^2*x^2)
//   2   初值的选取是根据bilinear curve近似所得(分段分析)；求根迭代过程
//       是根据Halley's method来iteration
//   3   算法引用的文献为：
//       1   Gooding,R.H.:: 1988a,'On the Solution of Lambert's Orbital
//           Boundary-Value Problem',RAE Technical Report 88027
//       2   Gooding,R.H.:: 1989, 'A Procedure for the Solution of Lambert's 
//           Orbital Boundary
//--------------------------------------------------------------------

/// <summary>
/// Lambert 无量纲方程x的求解 (R.H.Gooding 方法)
/// </summary>
/// <param name="m">飞行的圈数(0 for 0-2pi)</param>
/// <param name="q">q=sqrt(r1*r2)/s*cos(0.5*theta)</param>
/// <param name="qsqfm1">(1-q*q): c/s</param>
/// <param name="tin">无量纲飞行时间T</param>
/// <param name="n">out:-1:异常; 0:无解(m>0,T<Tm); 1:1个解; 2:2个解(m>0)</param>
/// <param name="x">out:解1</param>
/// <param name="xpl">out:解2</param>
static void XLAMB(int m, double q, double qsqfm1, double tin, out int n, out double x, out double xpl)
{
    double dt, d2t, d3t;
    //  系数
    double tol = 3.0e-7;
    double c0 = 1.7;
    double c1 = 0.5;
    double c2 = 0.03;
    double c3 = 0.15;
    double c41 = 1.0;
    double c42 = 0.24;
    //  初始化
    x = xpl = 0.0;
    double t0 = 0.0;        //T(x=0)
    double t = 0.0;

    double tmin = 0.0;      //多圈时,T的最小值
    double xm = 0.0;        //多圈时，T最小值的x
    double tdiffm = 0;      //tin-tmin
    double d2t2 = 0.0;      //T的二阶导数(x=xm)/2

    //  theta/2pi
    double thr2 = Math.Atan2(qsqfm1, 2.0 * q) / Math.PI;

    #region  1圈内转移,x的初值(x>-1; 可能为 椭圆，抛物线，双曲线)
    if (m == 0)
    {
        //  x仅有一个解
        //  Single-rev starter from  t (at x = 0) & bilinear (usually)
        n = 1;
        //  求x=0时对应的T，以此判断x是否大于0，T随x单调减
        TLAMB(m, q, qsqfm1, 0.0, 0, out t0, out dt, out d2t, out d3t);

        double tdiff = tin - t0;

        //当 x>0(tin <= t0) 时(bilinear curve拟合产生初始x0)
        if (tdiff <= 0.0)
        {
            x = t0 * tdiff / (-4.0 * tin);
        }
        //当 x<0(tin > t0) 时 (bilinear curve(Need patch)拟合产生初始x0)
        // (-4 is the value of dt, for x = 0)
        else
        {
            x = -tdiff / (tdiff + 4.0);
            double w = x + c0 * Math.Sqrt(2.0 * (1.0 - thr2));

            if (w < 0.0)
                x = x - Math.Sqrt(d8rt(-w)) * (x + Math.Sqrt(tdiff / (tdiff + 1.5 * t0)));

            w = 4.0 / (4.0 + tdiff);
            x = x * (1.0 + x * (c1 * w - c2 * x * Math.Sqrt(w)));
        }
    }
    #endregion

    #region 多圈转移,x的初值(|x|<1,仅有椭圆情形)
    else
    {
        //首先求出m圈转移中对应最小时间Tmin的Xm

        //xm初值的选取                
        xm = 1.0 / (1.5 * (m + 0.5) * Math.PI);
        if (thr2 < 0.5) xm = d8rt(2.0 * thr2) * xm;
        if (thr2 > 0.5) xm = (2.0 - d8rt(2.0 - 2.0 * thr2)) * xm;

        #region 在12个循环内迭代找到tmin,及xm (Halley's method for iteration)
        d2t = 0;
        int i = 0;
        while (i++ < 12)
        {
            //  求解xm对应的tmin及其1-3阶导数
            TLAMB(m, q, qsqfm1, xm, 3, out tmin, out dt, out d2t, out d3t);

            if (d2t == 0) break;    //  当q=1时,d2t=0,此时xm=0,停止迭代

            double xmold = xm;
            xm = xm - dt * d2t / (d2t * d2t - dt * d3t / 2.0);  //Halley迭代,更新xm

            //若xm相对改变小于tol,则认为找到Xm,停止迭代
            if (Math.Abs(xmold / xm - 1.0) <= tol) break;
        }

        //找不到xm,令n=-1,然后返回!( 此种情况不应该发生)
        if (i > 11)
        {
            n = -1;
            return;
        }
        #endregion

        tdiffm = tin - tmin;

        //   当 tin < tmin 时，无解(N=0),程序退出
        if (tdiffm < 0.0)
        {
            n = 0;
            return;
        }
        //  当 tin = tmin 时，仅有1解(N=1),程序退出
        if (tdiffm == 0.0)
        {
            x = xm;
            n = 1;
            return;
        }
        //  当 tin > tmin 时，有两解

        n = 2;
        if (d2t == 0) d2t = 6.0 * m * Math.PI;
        d2t2 = d2t / 2.0;       //  T的二阶导数(x=xm时)/2

        #region 求出x>xm时的初值xpl                    
        x = Math.Sqrt(tdiffm / (d2t / 2.0 + tdiffm / (1.0 - xm) / (1.0 - xm)));
        double w = xm + x;
        w = w * 4.0 / (4.0 + tdiffm) + (1.0 - w) * (1.0 - w);
        x = x * (1.0 - (1.0 + m + c41 * (thr2 - 0.5)) / (1.0 + c3 * m) * x * (c1 * w + c2 * x * Math.Sqrt(w))) + xm;
        xpl = x;

        //  若x>1,则x>xm时没有解
        if (x >= 1.0)
            n = 1;
        #endregion

        #region 求出x<xm时的初值x
        //  m>0,x=0时的T
        TLAMB(m, q, qsqfm1, 0, 0, out t0, out dt, out d2t, out d3t);

        double tdiff0 = t0 - tmin;
        double tdiff = tin - t0;

        //  tmin < tin <t0 的情形
        if (tdiff <= 0)
        {
            x = xm - Math.Sqrt(tdiffm / (d2t2 - tdiffm * (d2t2 / tdiff0 - 1.0 / xm / xm)));
        }
        //  tin > t0 的情形
        else
        {
            x = -tdiff / (tdiff + 4.0);
            w = x + c0 * Math.Sqrt(2.0 * (1.0 - thr2));
            if (w < 0.0) x = x - Math.Sqrt(d8rt(-w)) * (x + Math.Sqrt(tdiff / (tdiff + 1.5 * t0)));
            w = 4.0 / (4.0 + tdiff);
            x = x * (1.0 + (1.0 + m + c42 * (thr2 - 0.5)) / (1.0 + c3 * m) * x * (c1 * w - c2 * x * Math.Sqrt(w)));
        }

        if (x <= -1.0)  //若x<-1,则x<xm时没有解                           
        {
            x = xpl;    //使用x02赋值当前x
            n = n - 1;
        }
        #endregion

        if (n == 0) return;     //无解，则直接返回
    }
    #endregion

    //  由初值x进行三次迭代寻找到解(3次迭代保证精度); Haley's formula iteration
    for (int i = 1; i <= 3; i++)
    {
        TLAMB(m, q, qsqfm1, x, 2, out t, out dt, out d2t, out d3t);
        t = tin - t;

        if (dt != 0.0) x = x + t * dt / (dt * dt + t * d2t / 2.0);
    }

    if (n == 1) return;     //只有1个解直接返回

    //  若有两个解，求解第2个解，初值xpl
    //  由初值x进行三次迭代寻找到解(3次迭代保证精度); Haley's formula iteration
    for (int i = 1; i <= 3; i++)
    {
        TLAMB(m, q, qsqfm1, xpl, 2, out t, out dt, out d2t, out d3t);
        t = tin - t;

        if (dt != 0.0) xpl = xpl + t * dt / (dt * dt + t * d2t / 2.0);
    }
}

/// <summary>
/// 开8次方(x^(1/8))
/// </summary>
static double d8rt(double x)
{
    return Math.Sqrt(Math.Sqrt(Math.Sqrt(x)));
}
```

