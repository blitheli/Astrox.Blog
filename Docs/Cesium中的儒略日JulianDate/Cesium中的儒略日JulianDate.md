# Cesium中的儒略日JulianDate

在天文和卫星轨道计算中，有关儒略日的计算是一个绕不开的话题。本章阐述下Cesium中有关儒略日的处理方法。

涉及到的时间系统，如UTC，TAI等，这里不再详细阐述，读者自行阅读专业书籍或者网上查询。

Cesium中，使用JulianDate模块来处理儒略日的计算，具体为Cesium软件包目录中的文件："\Source\Core\JulianDate.js"。
## 几种时间系统
首先简要阐述下几个不同时间系统的概念。

 1. UTC（coordinated universal time），也称协调时间时。也就是我们现在日常使用的时间。
 2. TAI（International Atomic Time），也称原子时。
 3. DAT，跳秒，或者闰秒，是UTC和TAI之间的差别。
 4. TT（terrestrial time），或者为TDT，也称地球动力学时。
 5. UT1（Universal Time1），也称格林威治平太阳时，在UT0的基础上加入了极移修正。是以地球自转为基准得到的，也是目前使用的世界时标准。

以上几个时间系统中，UT1与地球自转紧密相连，因此不是均匀的。其余诸如UTC，TAI和TT是均匀流逝的，有点像牛顿的绝对时空观里的时间。

不同场合使用不同的时间系统，就像阴历与阳历一样，都是对时间的计数方式而已，我们要弄清楚的就是他们之间的转换关系。

UTC时间与UT1最为接近，且UTC是均匀流逝的，所以UTC作为我们日常使用的时间。但是由于地球的自转速度越转越慢，因此为了协调UT1时间和UTC时间，过个几年会有润秒的出现，即UTC时间减慢一秒，具体可搜索润秒的详细描述，此处不再详述。

下面给出上述几种时间的相互转换关系
$$UT1=UTC+(UT1-UTC) \\
DAT=TAI-UTC\\
TAI=UTC+DAT\\
TT=TAI+32.184s$$
上式中，最近的一次跳秒发生在2017年1月1日零时(UTC），因此目前跳秒数DAT为37s。

第一个式子有关UT1的写法可能会感到奇怪。前面说了，UT1的时间流逝是不均匀的，而UTC的时刻我们是知道的，因此IERS公报中给出UTC与UT1的差值：(UT1-UTC)，只要将这个差值加上UTC时间即可得到UT1。
## 儒略日的计算
儒略日（Julian day，JD）是指由公元前4713年1月1日，协调世界时中午12时开始所经过的天数，当然这个天数是包含小数点的。显然，儒略日使用自然累加的天数作为时间的计时方式，是一种均匀流逝的绝对时间。

儒略日的计算涉及到闰年、闰月的处理，计算公式各种程序语言版本的都有。读者可参考书籍中的算法》

注意，儒略日的计算公式只涉及到年月日时分秒的计算，并不管是UTC还是TAI时间。当是TAI时间时，则计算得到的儒略日是正确的；而当是UTC时间时，因为有跳秒的存在，计算得到的儒略日就是“假的”，因此需要主动加上跳秒。

例如：
UTC时间：2020年9月5日12:00:00s，对应的TAI时间为2020年9月5日12:00:37s。
因为我们知道，自2017年1月1日00:00:00s，跳秒为37s。

所以说，给定UTC时间，在转换为TAI时间时，必须知道跳秒数，即前面提到的$DAT$。
Cesium中，JulianDate模块中，维护了历次跳秒的时刻及跳秒数的数表(见下），在计算UTC向TAI转换时，需要查这个表，以便判断对应的跳秒数。

```
JulianDate.leapSeconds = [
  new LeapSecond(new JulianDate(2441317, 43210.0, TimeStandard.TAI), 10), // January 1, 1972 00:00:00 UTC
  new LeapSecond(new JulianDate(2441499, 43211.0, TimeStandard.TAI), 11), // July 1, 1972 00:00:00 UTC
  new LeapSecond(new JulianDate(2441683, 43212.0, TimeStandard.TAI), 12), // January 1, 1973 00:00:00 UTC
  new LeapSecond(new JulianDate(2442048, 43213.0, TimeStandard.TAI), 13), // January 1, 1974 00:00:00 UTC
  new LeapSecond(new JulianDate(2442413, 43214.0, TimeStandard.TAI), 14), // January 1, 1975 00:00:00 UTC
  new LeapSecond(new JulianDate(2442778, 43215.0, TimeStandard.TAI), 15), // January 1, 1976 00:00:00 UTC
  new LeapSecond(new JulianDate(2443144, 43216.0, TimeStandard.TAI), 16), // January 1, 1977 00:00:00 UTC
  new LeapSecond(new JulianDate(2443509, 43217.0, TimeStandard.TAI), 17), // January 1, 1978 00:00:00 UTC
  new LeapSecond(new JulianDate(2443874, 43218.0, TimeStandard.TAI), 18), // January 1, 1979 00:00:00 UTC
  new LeapSecond(new JulianDate(2444239, 43219.0, TimeStandard.TAI), 19), // January 1, 1980 00:00:00 UTC
  new LeapSecond(new JulianDate(2444786, 43220.0, TimeStandard.TAI), 20), // July 1, 1981 00:00:00 UTC
  new LeapSecond(new JulianDate(2445151, 43221.0, TimeStandard.TAI), 21), // July 1, 1982 00:00:00 UTC
  new LeapSecond(new JulianDate(2445516, 43222.0, TimeStandard.TAI), 22), // July 1, 1983 00:00:00 UTC
  new LeapSecond(new JulianDate(2446247, 43223.0, TimeStandard.TAI), 23), // July 1, 1985 00:00:00 UTC
  new LeapSecond(new JulianDate(2447161, 43224.0, TimeStandard.TAI), 24), // January 1, 1988 00:00:00 UTC
  new LeapSecond(new JulianDate(2447892, 43225.0, TimeStandard.TAI), 25), // January 1, 1990 00:00:00 UTC
  new LeapSecond(new JulianDate(2448257, 43226.0, TimeStandard.TAI), 26), // January 1, 1991 00:00:00 UTC
  new LeapSecond(new JulianDate(2448804, 43227.0, TimeStandard.TAI), 27), // July 1, 1992 00:00:00 UTC
  new LeapSecond(new JulianDate(2449169, 43228.0, TimeStandard.TAI), 28), // July 1, 1993 00:00:00 UTC
  new LeapSecond(new JulianDate(2449534, 43229.0, TimeStandard.TAI), 29), // July 1, 1994 00:00:00 UTC
  new LeapSecond(new JulianDate(2450083, 43230.0, TimeStandard.TAI), 30), // January 1, 1996 00:00:00 UTC
  new LeapSecond(new JulianDate(2450630, 43231.0, TimeStandard.TAI), 31), // July 1, 1997 00:00:00 UTC
  new LeapSecond(new JulianDate(2451179, 43232.0, TimeStandard.TAI), 32), // January 1, 1999 00:00:00 UTC
  new LeapSecond(new JulianDate(2453736, 43233.0, TimeStandard.TAI), 33), // January 1, 2006 00:00:00 UTC
  new LeapSecond(new JulianDate(2454832, 43234.0, TimeStandard.TAI), 34), // January 1, 2009 00:00:00 UTC
  new LeapSecond(new JulianDate(2456109, 43235.0, TimeStandard.TAI), 35), // July 1, 2012 00:00:00 UTC
  new LeapSecond(new JulianDate(2457204, 43236.0, TimeStandard.TAI), 36), // July 1, 2015 00:00:00 UTC
  new LeapSecond(new JulianDate(2457754, 43237.0, TimeStandard.TAI), 37), // January 1, 2017 00:00:00 UTC
];
```
## JulianDate的结构和计算
Cesium中，使用JulianDate模块包含了相关的计算函数。
可以使用构造函数可以创建一个JulianDate对象，对象内部仅包含两个属性，且仅对应TAI时间：

 - dayNumber: 保留儒略日的整数部分，单位为天；
 - secondsOfDay:保留儒略日的小数部分，单位为秒；
 
之所以用两个参数来表示儒略日，是为了最大可能的保证数据的精度。例如J2000时刻，儒略日为2451545天。光整数天就需要7位有效数字，如果表示到1ms的精度，则儒略日为：2451545.00000001天，可以看出超出了一般双精度15位有效数字。因此使用内部使用整数天和不足一天的秒数来保存，从而保证了儒略日数据的精度。

直接的构造函数如下：
```
function JulianDate(julianDayNumber, secondsOfDay, timeStandard) 
```
其中,timeStandard表示时间类型，只有两种选择:UTC和TAI。没有则默认为UTC。当为UTC类型时，构造函数内部需要将UTC时间转换为TAI时间（涉及到跳秒数表的查询）。

其它的构造函数有：JulianDate.fromGregorianDate、JulianDate.fromDate等。

下面给出了使用两种构造函数创建JulianDate对象的代码例子：
```
//  使用ES6 module, 引用Cesium代码包里的源代码文件
import JulianDate from "../Source/Core/JulianDate.js";
import GregorianDate from "../Source/Core/GregorianDate.js";

//  使用GregorianDate时间类型创建，默认为UTC时间
var jd1 = JulianDate.fromGregorianDate(new GregorianDate(2020,9,5,0,0,0,0));
console.log(jd1);

//  直接使用儒略日的整数天和一天内的秒数创建,默认为UTC时间
var jd2 =new JulianDate(2459097, 43200.0);
console.log(jd2);

/*  输出都是(TAI时间):
JulianDate {dayNumber: 2459097, secondsOfDay: 43237}
*/
```

