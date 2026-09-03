package;

class TestCalls extends Base implements Sized {
    public var items:Array<Int>;

    public function new(label:String) {
        super(label);

        this.items = [];
    }

    public function size():Int {
        return this.items.length;
    }

    override public function name():String {
        return "call " + super.name();
    }

    public static function make(label:String):TestCalls {
        return new TestCalls(label);
    }

    public function statics():String {
        return TestProps.shared + Std.string(TestCalls.make("x").size());
    }

    public function members(other:Base):String {
        return other.name() + this.name();
    }

    public function indexing(i:Int):Int {
        this.items[i] = this.items[i] + 1;
        return this.items[i];
    }

    public function closures(n:Int):Int {
        var scale = n * 2;

        var add = function(a:Int, b:Int):Int {
            return a + b + scale;
        };

        var noArgs = function() {
            return scale;
        };

        var withDefault = function(a:Int = 5) {
            if (a > 0) {
                return a;
            }
            return 0;
        };

        return add(1, 2) + noArgs() + withDefault();
    }

    public function casts(v:Dynamic):String {
        var b:Base = cast(v, Base);
        var d:Dynamic = this;
        var i:Int = Std.int(1.9);
        var f:Float = i;
        return b.name() + Std.string(d) + Std.string(i) + Std.string(f);
    }

    public function optionals(a:Int, ?b:String, c:Bool = true, d:Float = 1.5):String {
        return a + Std.string(b) + Std.string(c) + Std.string(d);
    }

    public function classRef():Class<Dynamic> {
        return TestCalls;
    }
}
