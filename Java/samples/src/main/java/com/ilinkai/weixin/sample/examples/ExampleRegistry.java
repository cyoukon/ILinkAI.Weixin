package com.ilinkai.weixin.sample.examples;

import java.util.LinkedHashMap;
import java.util.Map;

public class ExampleRegistry {
    private final Map<String, IExample> examples = new LinkedHashMap<>();

    public ExampleRegistry() {
        register(new LoginExample());
        register(new SendExample());
        register(new MonitorExample());
        register(new AccountExample());
        register(new UploadExample());
        register(new DownloadExample());
        register(new ConfigExample());
        register(new TypingExample());
        register(new FullWorkflowExample());
    }

    public void register(IExample example) {
        examples.put(example.getName().toLowerCase(), example);
    }

    public IExample getExample(String name) {
        return examples.get(name.toLowerCase());
    }

    public void printUsage() {
        System.out.println("用法: java -jar sample.jar <命令> [参数]");
        System.out.println();
        System.out.println("命令:");
        for (IExample ex : examples.values()) {
            System.out.printf("  %-18s- %s%n", ex.getName(), ex.getDescription());
        }
        System.out.println();
        System.out.println("示例:");
        System.out.println("  java -jar sample.jar login");
        System.out.println("  java -jar sample.jar send --to user@im.wechat --text \"Hello\" --token xxx --context-token xxx");
        System.out.println("  java -jar sample.jar monitor --account-id xxx --token xxx");
        System.out.println("  java -jar sample.jar account list");
    }
}
