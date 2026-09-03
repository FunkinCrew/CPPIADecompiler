package;

class Base implements Named {
    public var label:String;

    public function new(label:String = "base") {
        this.label = label;
    }

    public function name():String {
        return this.label;
    }
}
