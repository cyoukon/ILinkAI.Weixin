package com.ilinkai.weixin.sample.examples;

public interface IExample {
    String getName();
    String getDescription();
    String getUsage();
    int execute(String[] args) throws Exception;
}
