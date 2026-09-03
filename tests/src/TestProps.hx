package;

class TestProps {
    public var plain:Int;
    public var readOnly(default, null):String;

    @:isVar
    public var counted(get, set):Int;

    var backing:Int;

    public static var shared:String = "static init";
    public static var table:Array<Int> = [1, 2, 3];
    public static var refs:Array<Class<Dynamic>> = [Base, TestProps];

    public dynamic function hook(v:Int):Int {
        return v;
    }

    public function new() {
        this.plain = 0;
        this.readOnly = "fixed";
        this.backing = 0;
        this.counted = 1;
    }

    function get_counted():Int {
        return this.backing;
    }

    function set_counted(v:Int):Int {
        this.backing = v;
        return v;
    }
}
