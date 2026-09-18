# 轨道力学基本子程序(Fortran)

前些年我自己编写的轨道力学基本子程序，是Fortran版本的，有需要的可以看看。

http://download.csdn.net/download/u011575168/9631112

包含的文件:
|文件名|主要内容|
|-------|---------|
|Basic_RV_Elements.f90|二体意义下，二次曲线轨道 位置速度 与 轨道根数的相互转换|
|Basic_KeplerEquation.f90|	椭圆，双曲线轨道 Kepler方程的 求解|
|Basic_Lambert.f90|		二体意义下： Lambert方程的求解|
|Basic_SattOrbit.f90|		卫星轨道 基本子程序|
|Basic_OrbitTransfer.f90|		轨道转移 相关子程序|
|Basic_TansfMatrix.f90|		各坐标系转换子程序(主要用于火箭发射弹道计算)|
|Basic_Math.f90|			常用数学子程序库|
|Basic_Planet.f90|		行星位置速度，根数等 相关子程序|
|Basic_GravityAssist.f90|		行星引力加速 相关子程序|
|Basic_Optim.f90|			数值最优化|
|Basic_RKF78.f90|			常微分方程ODE RKF7(8)积分器|
|Basic_Eular2.f90|		常微分方程ODE EULAR 2阶积分器|
|Basic_GravityAccel.F90|          行星引力场模型(地固系下引力加速度)|
|Basic_CentralBody_Facility.f90|  行星基本参数&地面站|
