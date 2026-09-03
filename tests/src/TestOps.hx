package;

class TestOps {
    public function new() {}

    public function arithmetic(a:Int, b:Int):Int {
        var r = a + b;
        r = r - b;
        r = r * 2;
        r = Std.int(r / 2);
        r = r % 7;
        r = r & 0xFF;
        r = r | 1;
        r = r ^ 2;
        r = r << 1;
        r = r >> 1;
        r = r >>> 1;
        r = -r;
        r = ~r;
        return r;
    }

    public function compound(a:Int):Int {
        a += 1;
        a -= 2;
        a *= 3;
        a %= 5;
        a &= 0xF;
        a |= 2;
        a ^= 1;
        a <<= 1;
        a >>= 1;
        return a;
    }

    public function crements(a:Int):Int {
        var x = a;
        x++;
        ++x;
        x--;
        --x;
        return x;
    }

    public function logic(a:Int, b:Int, s:String):Bool {
        var ok = a < b && b > a || a <= b && b >= a;
        ok = ok && a == b;
        ok = ok || a != b;
        ok = !ok;
        ok = ok && s != null;
        ok = ok || s == null;
        return ok;
    }

    public function precedence(a:Int, b:Int, c:Int):Int {
        return a + b * c - (a + b) * c;
    }

    public function ternary(a:Int):String {
        return a > 0 ? "pos" : a < 0 ? "neg" : "zero";
    }

    public function ternaryValue(a:Int):String {
        var sign = a > 0 ? "pos" : "neg";
        var mark = a == 0 ? "!" : "?";
        return sign + mark;
    }

    public function literals():Dynamic {
        var i = 42;
        var f = 1.5;
        var s = "quotes \" and \\ and tab \t and newline \n done";
        var t = true;
        var n = null;
        var arr = [1, 2, 3];
        var obj = { alpha: 1, beta: "two", gamma: [3.0] };
        return { i: i, f: f, s: s, t: t, n: n, arr: arr, obj: obj };
    }
}
