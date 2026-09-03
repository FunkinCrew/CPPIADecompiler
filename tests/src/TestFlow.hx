package;

class TestFlow {
    public function new() {}

    public function branches(a:Int):String {
        if (a > 10) {
            return "big";
        }

        if (a > 5) {
            return "medium";
        } else {
            return "small";
        }
    }

    public function loops(n:Int):Int {
        var total = 0;

        for (i in 0...n) {
            if (i == 3) continue;
            if (i == 8) break;
            total += i;
        }

        var w = 0;
        while (w < n) {
            total += w;
            w++;
        }

        do {
            total++;
        } while (total < 0);

        for (v in [1, 2, 3]) {
            total += v;
        }

        return total;
    }

    public function iterated(m:Map<String, Int>, it:Iterator<Int>):Int {
        var total = 0;

        for (k in m.keys()) {
            total += m.get(k);
        }

        for (v in it) {
            total += v;
        }

        return total;
    }

    public function pick(a:Int):String {
        switch (a) {
            case 0:
                return "zero";
            case 1, 2:
                return "small";
            case 3:
                var extra = a * 2;
                return "three " + extra;
            default:
                return "other";
        }
    }

    public function guarded(a:Int):String {
        try {
            if (a < 0) throw "negative";
            return "fine";
        } catch (e:String) {
            return "caught " + e;
        } catch (e:Dynamic) {
            return "unknown";
        }
    }

    public function shapes(s:Shape):Float {
        return switch (s) {
            case Dot: 0;
            case Line(len): len;
            case Box(w, h): w * h;
        }
    }

    public function makeShapes():Array<Shape> {
        return [Dot, Line(2.5), Box(1, 2)];
    }
}
