package;

class TestMain {
    static function main() {
        var props = new TestProps();
        var ops = new TestOps();
        var flow = new TestFlow();
        var calls = new TestCalls("main");
        var extra = new TestExtra();

        trace(props.counted);
        trace(ops.arithmetic(1, 2));
        trace(flow.loops(10));
        trace(calls.closures(3));
        trace(flow.shapes(Box(2, 3)));
        trace(extra.usesInline(2));
        trace(extra.globals());
    }
}
