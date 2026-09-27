# Cesium中的相机—四元素

有关四元素和矩阵转换的具体定义和过程请参考相关资料，本文直接给出具体的结果。

维基百科上的参考资料：
https://en.wikipedia.org/wiki/Conversion_between_quaternions_and_Euler_angles
## 四元素的定义
在[Cesium中的相机—旋转矩阵](/Posts/cesium%E4%B8%AD%E7%9A%84%E7%9B%B8%E6%9C%BA%E6%97%8B%E8%BD%AC%E7%9F%A9%E9%98%B5)一文中，我们用常用绕 XYZ 三个轴的旋转来表示坐标系的三维旋转，对应的旋转角度即欧拉角。采用欧拉角描述旋转的优点是直观简单（矩阵乘），容易理解，旋转过程分解为：按照一定的顺序（比如X、Y、Z） 依次独立地绕轴旋转。

这种独立的旋转带来了一个问题，也就是未考虑到旋转之间的关联性，实际上任意两个坐标系之间（共原点，仅考虑旋转不考虑平移）的关系都可通过一次旋转完成。

假设原始坐标系为$o-xyz$，绕旋转轴（旋转轴矢量为$[x,y,z]^T$）旋转$\alpha$角度，得到最终的坐标系$o-XYZ$，则此旋转可以用单位四元素$q$来表示：

$$
q=
\begin{pmatrix} q_0 \\ q_1 \\ q_2 \\ q_3 \end{pmatrix}
=
\begin{pmatrix} q_w \\ q_x \\ q_y \\ q_z \end{pmatrix}
=
\begin{pmatrix}
\cos(\alpha/2) \\
\sin(\alpha/2)x \\
\sin(\alpha/2)y \\
\sin(\alpha/2)z
\end{pmatrix}
\tag{1}
$$

有：

$$
q_0^2+q_1^2+q_2^2+q_3^2=q_w^2+q_x^2+q_y^2+q_z^2=1
$$

## 四元素的矩阵表示
给定四元素（即旋转轴和旋转角度），则对应的坐标轴旋转矩阵也可写出，这里仍分别写出两种形式。
### 第一种旋转矩阵(仅坐标系旋转)
若以$\begin{pmatrix} x,y,z\end{pmatrix}^{T}$表示点P在原坐标系$o-xyz$中的坐标分量，$\begin{pmatrix} x',y',z'\end{pmatrix}^{T}$表示点P在旋转后的坐标系$o-XYZ$中的坐标分量，则有：

$$
\begin{pmatrix} x' \\ y' \\ z' \end{pmatrix}
=
M(q)\cdot\begin{pmatrix} x \\ y \\ z \end{pmatrix}
\tag{2}
$$

其中，由四元素表示的旋转矩阵$M$形式为：

$$
M(q)=
\begin{pmatrix}
q_0^{2}+q_1^2-q_2^2-q_3^2 & 2(q_1q_2+q_0q_3) & 2(q_1q_3-q_0q_2) \\
2(q_1q_2-q_0q_3) & q_0^2-q_1^2+q_2^2-q_3^2 & 2(q_0q_1+q_2q_3) \\
2(q_0q_2+q_1q_3) & 2(q_2q_3-q_0q_1) & q_0^2-q_1^2-q_2^2+q_3^2
\end{pmatrix}
\tag{3}
$$

**旋转矩阵$M$是将点P在原坐标系中的坐标分量转换到新坐标系中的坐标分量**。
### 第二种旋转矩阵(点或矢量随坐标系一起旋转)
以$\begin{pmatrix} x,y,z\end{pmatrix}^{T}$表示点P在旋转后坐标系$o-XYZ$中的坐标分量（始终不变），$\begin{pmatrix} x',y',z'\end{pmatrix}^{T}$表示点P旋转后在原坐标系$o-xyz$中的坐标分量，则有：

$$
\begin{pmatrix} x' \\ y' \\ z' \end{pmatrix}
=
M(q)\cdot\begin{pmatrix} x \\ y \\ z \end{pmatrix}
\tag{4}
$$

其中，由四元素表示的旋转矩阵$M$形式为：

$$
M(q)=
\begin{pmatrix}
q_0^2+q_1^2-q_2^2-q_3^2 & 2(q_1q_2-q_0q_3) & 2(q_0q_2+q_1q_3) \\
2(q_1q_2+q_0q_3) & q_0^2-q_1^2+q_2^2-q_3^2 & 2(q_2q_3-q_0q_1) \\
2(q_1q_3-q_0q_2) & 2(q_0q_1+q_2q_3) & q_0^2-q_1^2-q_2^2+q_3^2
\end{pmatrix}
\tag{5}
$$

点P始终随坐标系$o-XYZ$一起旋转，因此坐标分量始终为$\begin{pmatrix} x,y,z\end{pmatrix}^{T}$

**旋转矩阵$M$是将点P在旋转后坐标系$o-XYZ$（也可看成体坐标系）中的坐标分量转换到旋转前的原坐标系中的坐标分量**。

**注意！上述两式中，基础旋转矩阵刚好互逆，即式（3）和式（5）中的$M$是不同的，刚好互为转置。

**Cesium中，采用第二种旋转矩阵的形式！**
## 四元素的乘法
**这里仅仅给出第二种旋转矩阵(点或矢量随坐标系一起旋转)的四元素乘法。**
- 假设原始坐标系为$o-xyz$，绕旋转轴（旋转轴矢量为$[x_1,y_1,z_1]^T$）旋转$\alpha_1$角度，得到最终的坐标系$o-X'Y'Z'$，定义此旋转的四元素为$q_1$
- 接着再从坐标系$o-X'Y'Z'$开始，绕旋转轴（旋转轴矢量为$[x_2,y_2,z_2]^T$）旋转$\alpha_2$角度，得到最终的坐标系$o-XYZ$，定义此旋转的四元素为$q_2$
- 则从原坐标系$o-xyz$到最终的坐标系$o-XYZ$的旋转可以用一次旋转完整，其四元素$q$可表示为$q_1$和$q_2$的乘积，即：

$$
q_1\cdot q_2=
\begin{pmatrix} w_1 \\ x_1 \\ y_1 \\ z_1 \end{pmatrix}
\cdot
\begin{pmatrix} w_2 \\ x_2 \\ y_2 \\ z_2 \end{pmatrix}
=
\begin{pmatrix}
w_1w_2 -x_1x_2-y_1y_2-z_1z_2 \\
w_1 x_2+x_1 w_2+y_1 z_2-z_1 y_2 \\
w_1 y_2-x_1 z_2+y_1 w_2+z_1 x_2 \\
w_1 z_2+x_1 y_2-y_1 x_2+z_1 w_2
\end{pmatrix}
\tag{6}
$$

某点P经两次旋转后，在原坐标系$o-xyz$中的坐标$\begin{pmatrix} x',y',z'\end{pmatrix}^{T}$为：

$$
\begin{aligned}
\begin{pmatrix} x' \\ y' \\ z' \end{pmatrix}
&=
M(q_1)\cdot M(q_2) \cdot\begin{pmatrix} x \\ y \\ z \end{pmatrix} \\
&=
M(q_1\cdot q_2) \cdot\begin{pmatrix} x \\ y \\ z \end{pmatrix}
\end{aligned}
\tag{7}
$$

## 小结
在使用四元素表示两个坐标系之间的旋转关系时，需要注意四元素转换为旋转矩阵的具体形式，是第一类旋转（式（3））还是第二类旋转式（5）。

此外，连续两个四元素的乘积也有区别的，本文仅仅给出第二类旋转方式。

此处再次强调，**Cesium中，采用第二种旋转矩阵的形式！**
