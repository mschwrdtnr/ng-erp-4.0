﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Master40.Tools.DistributionProvider
{
    public static class Quantiles
    {
        public static List<Quantil> GetAll { get; } = new List<Quantil>()
        {
            new Quantil(0.9999, 3.719),
            new Quantil(0.9998, 3.5401),
            new Quantil(0.9997, 3.4316),
            new Quantil(0.9996, 3.3528),
            new Quantil(0.9995, 3.2905),
            new Quantil(0.9994, 3.2389),
            new Quantil(0.9993, 3.1946),
            new Quantil(0.9992, 3.1559),
            new Quantil(0.9991, 3.1214),
            new Quantil(0.999, 3.0902),
            new Quantil(0.9989, 3.0618),
            new Quantil(0.9988, 3.0357),
            new Quantil(0.9987, 3.0115),
            new Quantil(0.9986, 2.9889),
            new Quantil(0.9985, 2.9677),
            new Quantil(0.9984, 2.9478),
            new Quantil(0.9983, 2.929),
            new Quantil(0.9982, 2.9112),
            new Quantil(0.9981, 2.8943),
            new Quantil(0.998, 2.8782),
            new Quantil(0.9979, 2.8627),
            new Quantil(0.9978, 2.848),
            new Quantil(0.9977, 2.8338),
            new Quantil(0.9976, 2.8202),
            new Quantil(0.9975, 2.807),
            new Quantil(0.9974, 2.7944),
            new Quantil(0.9973, 2.7821),
            new Quantil(0.9972, 2.7703),
            new Quantil(0.9971, 2.7589),
            new Quantil(0.997, 2.7478),
            new Quantil(0.9969, 2.737),
            new Quantil(0.9968, 2.7266),
            new Quantil(0.9967, 2.7164),
            new Quantil(0.9966, 2.7065),
            new Quantil(0.9965, 2.6968),
            new Quantil(0.9964, 2.6874),
            new Quantil(0.9963, 2.6783),
            new Quantil(0.9962, 2.6693),
            new Quantil(0.9961, 2.6606),
            new Quantil(0.996, 2.6521),
            new Quantil(0.9955, 2.6121),
            new Quantil(0.995, 2.5758),
            new Quantil(0.9945, 2.5427),
            new Quantil(0.994, 2.5121),
            new Quantil(0.9935, 2.4838),
            new Quantil(0.993, 2.4573),
            new Quantil(0.9925, 2.4324),
            new Quantil(0.992, 2.4089),
            new Quantil(0.9915, 2.3867),
            new Quantil(0.991, 2.3656),
            new Quantil(0.9905, 2.3455),
            new Quantil(0.99, 2.3263),
            new Quantil(0.9895, 2.308),
            new Quantil(0.989, 2.2904),
            new Quantil(0.9885, 2.2734),
            new Quantil(0.988, 2.2571),
            new Quantil(0.9875, 2.2414),
            new Quantil(0.987, 2.2262),
            new Quantil(0.9865, 2.2115),
            new Quantil(0.986, 2.1973),
            new Quantil(0.9855, 2.1835),
            new Quantil(0.985, 2.1701),
            new Quantil(0.9845, 2.1571),
            new Quantil(0.984, 2.1444),
            new Quantil(0.9835, 2.1321),
            new Quantil(0.983, 2.1201),
            new Quantil(0.9825, 2.1084),
            new Quantil(0.982, 2.0969),
            new Quantil(0.9815, 2.0858),
            new Quantil(0.981, 2.0749),
            new Quantil(0.9805, 2.0642),
            new Quantil(0.98, 2.0537),
            new Quantil(0.9795, 2.0435),
            new Quantil(0.979, 2.0335),
            new Quantil(0.9785, 2.0237),
            new Quantil(0.978, 2.0141),
            new Quantil(0.9775, 2.0047),
            new Quantil(0.977, 1.9954),
            new Quantil(0.9765, 1.9863),
            new Quantil(0.976, 1.9774),
            new Quantil(0.975, 1.96),
            new Quantil(0.97, 1.8808),
            new Quantil(0.965, 1.8119),
            new Quantil(0.96, 1.7507),
            new Quantil(0.955, 1.6954),
            new Quantil(0.95, 1.6449),
            new Quantil(0.945, 1.5982),
            new Quantil(0.94, 1.5548),
            new Quantil(0.935, 1.5141),
            new Quantil(0.93, 1.4758),
            new Quantil(0.925, 1.4395),
            new Quantil(0.92, 1.4051),
            new Quantil(0.915, 1.3722),
            new Quantil(0.91, 1.3408),
            new Quantil(0.905, 1.3106),
            new Quantil(0.9, 1.2816),
            new Quantil(0.895, 1.2536),
            new Quantil(0.89, 1.2265),
            new Quantil(0.885, 1.2004),
            new Quantil(0.88, 1.175),
            new Quantil(0.875, 1.1503),
            new Quantil(0.87, 1.1264),
            new Quantil(0.865, 1.1031),
            new Quantil(0.86, 1.0803),
            new Quantil(0.855, 1.0581),
            new Quantil(0.85, 1.0364),
            new Quantil(0.845, 1.0152),
            new Quantil(0.84, 0.9945),
            new Quantil(0.835, 0.9741),
            new Quantil(0.83, 0.9542),
            new Quantil(0.825, 0.9346),
            new Quantil(0.82, 0.9154),
            new Quantil(0.815, 0.8965),
            new Quantil(0.81, 0.8779),
            new Quantil(0.805, 0.8596),
            new Quantil(0.8, 0.8416),
            new Quantil(0.795, 0.8239),
            new Quantil(0.79, 0.8064),
            new Quantil(0.785, 0.7892),
            new Quantil(0.78, 0.7722),
            new Quantil(0.77, 0.7388),
            new Quantil(0.76, 0.7063),
            new Quantil(0.75, 0.6745),
            new Quantil(0.74, 0.6433),
            new Quantil(0.73, 0.6128),
            new Quantil(0.72, 0.5828),
            new Quantil(0.71, 0.5534),
            new Quantil(0.7, 0.5244),
            new Quantil(0.69, 0.4959),
            new Quantil(0.68, 0.4677),
            new Quantil(0.67, 0.4399),
            new Quantil(0.66, 0.4125),
            new Quantil(0.65, 0.3853),
            new Quantil(0.64, 0.3585),
            new Quantil(0.63, 0.3319),
            new Quantil(0.62, 0.3055),
            new Quantil(0.61, 0.2793),
            new Quantil(0.6, 0.2533),
            new Quantil(0.59, 0.2275),
            new Quantil(0.58, 0.2019),
            new Quantil(0.57, 0.1764),
            new Quantil(0.56, 0.151),
            new Quantil(0.55, 0.1257),
            new Quantil(0.54, 0.1004),
            new Quantil(0.53, 0.0753),
            new Quantil(0.52, 0.0502),
            new Quantil(0.51, 0.0251),
            new Quantil(0.5, 0),
        };
        public static Quantil GetFor(double tolerance) 
                   => GetAll.OrderBy(x => Math.Abs(x.Tolerance - tolerance)).First();
    }
}
