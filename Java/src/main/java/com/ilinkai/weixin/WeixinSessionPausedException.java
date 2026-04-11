package com.ilinkai.weixin;

/** 微信会话暂停异常 */
public class WeixinSessionPausedException extends Exception {
    private final String accountId;
    private final int remainingMinutes;

    public WeixinSessionPausedException(String accountId, int remainingMinutes) {
        super("Session paused for accountId=" + accountId + ", " + remainingMinutes + " min remaining (errcode -14)");
        this.accountId = accountId;
        this.remainingMinutes = remainingMinutes;
    }

    public String getAccountId() { return accountId; }
    public int getRemainingMinutes() { return remainingMinutes; }
}
