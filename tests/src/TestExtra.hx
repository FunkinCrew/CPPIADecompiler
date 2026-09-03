package;

class TestExtra {
    public var known:Int;

    public function new() {
        this.known = 1;
    }

    public inline function doubled(a:Int):Int {
        return a * 2;
    }

    public function usesInline(a:Int):Int {
        return doubled(a) + doubled(a + 1);
    }

    public function globals():Float {
        return untyped __global__.__hxcpp_time_stamp();
    }

    public function resolve(name:String):Int {
        return this.known;
    }
}
