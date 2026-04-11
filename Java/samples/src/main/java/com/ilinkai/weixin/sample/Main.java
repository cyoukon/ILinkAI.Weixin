package com.ilinkai.weixin.sample;

import com.ilinkai.weixin.sample.examples.ExampleRegistry;

public class Main {
    public static void main(String[] args) {
        System.out.println("========================================");
        System.out.println("  ILinkai Weixin SDK 使用示例");
        System.out.println("========================================");
        System.out.println();

        if (args.length == 0) {
            ExampleRegistry registry = new ExampleRegistry();
            registry.printUsage();
            return;
        }

        String command = args[0].toLowerCase();

        try {
            ExampleRegistry registry = new ExampleRegistry();
            var example = registry.getExample(command);

            if (example != null) {
                int result = example.execute(args);
                System.exit(result);
            } else {
                registry.printUsage();
            }
        } catch (Exception ex) {
            System.err.println("执行命令时发生错误: " + ex.getMessage());
            ex.printStackTrace();
            System.exit(1);
        }
    }
}
