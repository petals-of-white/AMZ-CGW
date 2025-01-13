using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Models;

public readonly record struct RGB<TPixel> (TPixel R, TPixel G, TPixel B);