package com.ilinkai.weixin.sample.examples;

public abstract class ExampleBase implements IExample {

    protected void printHeader(String title) {
        System.out.println(">>> " + title);
        System.out.println();
    }

    protected void printSuccess(String message) {
        System.out.println("✅ " + message);
    }

    protected void printError(String message) {
        System.out.println("❌ " + message);
    }

    protected void printWarning(String message) {
        System.out.println("⚠️ " + message);
    }

    protected static String parseArgument(String[] args, String name) {
        for (int i = 0; i < args.length - 1; i++) {
            if (args[i].equalsIgnoreCase(name)) {
                return args[i + 1];
            }
        }
        return null;
    }
}
