<template>
	<el-config-provider :size="getGlobalComponentSize" :locale="getGlobalI18n">
		<router-view v-show="setLockScreen" />
		<LockScreen v-if="themeConfig.isLockScreen" />
		<Setings ref="setingsRef" v-show="setLockScreen" />
		<CloseFull v-if="!themeConfig.isLockScreen" />
		<!-- <Upgrade v-if="getVersion" /> -->
		<!-- <Sponsors /> -->
	</el-config-provider>
</template>

<script setup lang="ts" name="app">
import { defineAsyncComponent, computed, ref, onBeforeMount, onMounted, onUnmounted, nextTick, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue-i18n';
import { storeToRefs } from 'pinia';
import { useTagsViewRoutes } from '/@/stores/tagsViewRoutes';
import { useThemeConfig } from '/@/stores/themeConfig';
import other from '/@/utils/other';
import { Local, Session } from '/@/utils/storage';
import mittBus from '/@/utils/mitt';
import setIntroduction from '/@/utils/setIconfont';
import { useChangeColor } from '/@/utils/theme';

// 引入组件
const LockScreen = defineAsyncComponent(() => import('/@/layout/lockScreen/index.vue'));
const Setings = defineAsyncComponent(() => import('/@/layout/navBars/topBar/setings.vue'));
const CloseFull = defineAsyncComponent(() => import('/@/layout/navBars/topBar/closeFull.vue'));
const Upgrade = defineAsyncComponent(() => import('/@/layout/upgrade/index.vue'));
const Sponsors = defineAsyncComponent(() => import('/@/layout/sponsors/index.vue'));

// 定义变量内容
const { messages, locale } = useI18n();
const setingsRef = ref();
const route = useRoute();
const stores = useTagsViewRoutes();
const storesThemeConfig = useThemeConfig();
const { themeConfig } = storeToRefs(storesThemeConfig);

// 设置锁屏时组件显示隐藏
const setLockScreen = computed(() => {
	// 防止锁屏后，刷新出现不相关界面
	// https://gitee.com/lyt-top/vue-next-admin/issues/I6AF8P
	return themeConfig.value.isLockScreen ? themeConfig.value.lockScreenTime > 1 : themeConfig.value.lockScreenTime >= 0;
});
// 获取版本号
const getVersion = computed(() => {
	let isVersion = false;
	if (route.path !== '/login') {
		// @ts-ignore
		if ((Local.get('version') && Local.get('version') !== __NEXT_VERSION__) || !Local.get('version')) isVersion = true;
	}
	return isVersion;
});
// 获取全局组件大小
const getGlobalComponentSize = computed(() => {
	return other.globalComponentSize();
});
// 获取全局 i18n
const getGlobalI18n = computed(() => {
	return messages.value[locale.value];
});
// 设置初始化，防止刷新时恢复默认
onBeforeMount(() => {
	// 设置批量第三方 icon 图标
	setIntroduction.cssCdn();
	// 设置批量第三方 js
	setIntroduction.jsCdn();
});
// 页面加载时
onMounted(() => {
	nextTick(() => {
		mittBus.on('openSetingsDrawer', () => {
			setingsRef.value.openDrawer();
		});

		const { getLightColor, getDarkColor } = useChangeColor();
		const normalizeHex = (val: any, fallback: string) => {
			if (typeof val !== 'string') return fallback;
			const trimmed = val.trim();
			if (!trimmed) return fallback;
			const hex = trimmed.startsWith('#') ? trimmed : `#${trimmed}`;
			return /^\#[0-9A-Fa-f]{6}$/.test(hex) ? hex : fallback;
		};

		const forced = {
			primary: '#2299dd',
			menuBar: '#2299dd',
			columnsMenuBar: '#2299dd',
			menuBarActiveColor: '#e6f7ff',
		};

		const localThemeConfig = Local.get('themeConfig');
		const cfg: any = localThemeConfig ? { ...localThemeConfig } : { ...themeConfig.value };
		cfg.primary = normalizeHex(forced.primary, forced.primary);
		cfg.menuBar = normalizeHex(forced.menuBar, forced.menuBar);
		cfg.columnsMenuBar = normalizeHex(forced.columnsMenuBar, forced.columnsMenuBar);
		cfg.menuBarActiveColor = normalizeHex(forced.menuBarActiveColor, forced.menuBarActiveColor);
		cfg.topBar = normalizeHex(cfg.topBar, '#ffffff');
		cfg.topBarColor = normalizeHex(cfg.topBarColor, '#606266');
		cfg.menuBarColor = normalizeHex(cfg.menuBarColor, '#eaeaea');
		cfg.columnsMenuBarColor = normalizeHex(cfg.columnsMenuBarColor, '#e6e6e6');

		storesThemeConfig.setThemeConfig({ themeConfig: cfg });
		Local.set('themeConfig', cfg);

		document.documentElement.style.setProperty('--el-color-primary-dark-2', `${getDarkColor(cfg.primary, 0.1)}`);
		document.documentElement.style.setProperty('--el-color-primary', cfg.primary);
		for (let i = 1; i <= 9; i++) {
			document.documentElement.style.setProperty(`--el-color-primary-light-${i}`, `${getLightColor(cfg.primary, i / 10)}`);
		}

		document.documentElement.style.setProperty('--next-bg-menuBar', cfg.menuBar);
		document.documentElement.style.setProperty('--next-bg-menuBar-light-1', `${getLightColor(cfg.menuBar, 0.05)}`);
		document.documentElement.style.setProperty('--next-bg-menuBarColor', cfg.menuBarColor);
		document.documentElement.style.setProperty('--next-bg-menuBarActiveColor', cfg.menuBarActiveColor);
		document.documentElement.style.setProperty('--next-bg-columnsMenuBar', cfg.columnsMenuBar);
		document.documentElement.style.setProperty('--next-bg-columnsMenuBarColor', cfg.columnsMenuBarColor);
		document.documentElement.style.setProperty('--next-bg-topBar', cfg.topBar);
		document.documentElement.style.setProperty('--next-bg-topBarColor', cfg.topBarColor);
		document.documentElement.style.setProperty('--el-menu-bg-color', cfg.menuBar);

		Local.set('themeConfigStyle', document.documentElement.style.cssText);

		if (Session.get('isTagsViewCurrenFull')) {
			stores.setCurrenFullscreen(Session.get('isTagsViewCurrenFull'));
		}
	});
});
// 页面销毁时，关闭监听布局配置/i18n监听
onUnmounted(() => {
	mittBus.off('openSetingsDrawer', () => {});
});
// 监听路由的变化，设置网站标题
watch(
	() => route.path,
	() => {
		other.useTitle();
	},
	{
		deep: true,
	}
);
</script>
