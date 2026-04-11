package com.ilinkai.weixin.auth;

import com.ilinkai.weixin.WeixinSessionPausedException;
import java.util.HashMap;
import java.util.Map;

/** 会话守卫服务 - 管理会话状态和暂停逻辑 */
public class SessionGuard {
    private static final long SESSION_PAUSE_DURATION_MS = 60 * 60 * 1000L;
    public static final int SESSION_EXPIRED_ERROR_CODE = -14;

    private final Map<String, Long> pauseUntilMap = new HashMap<>();

    public void pauseSession(String accountId) {
        pauseUntilMap.put(accountId, System.currentTimeMillis() + SESSION_PAUSE_DURATION_MS);
    }

    public boolean isSessionPaused(String accountId) {
        Long until = pauseUntilMap.get(accountId);
        if (until == null) return false;
        if (System.currentTimeMillis() >= until) {
            pauseUntilMap.remove(accountId);
            return false;
        }
        return true;
    }

    public long getRemainingPauseMs(String accountId) {
        Long until = pauseUntilMap.get(accountId);
        if (until == null) return 0;
        long remaining = until - System.currentTimeMillis();
        if (remaining <= 0) {
            pauseUntilMap.remove(accountId);
            return 0;
        }
        return remaining;
    }

    public void assertSessionActive(String accountId) throws WeixinSessionPausedException {
        if (isSessionPaused(accountId)) {
            int remainingMin = (int) Math.ceil(getRemainingPauseMs(accountId) / 60000.0);
            throw new WeixinSessionPausedException(accountId, remainingMin);
        }
    }

    public void resetForTest() {
        pauseUntilMap.clear();
    }
}
