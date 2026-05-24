<template>
	<div ref="elRef" class="echarts-root" :style="rootStyle"></div>
</template>

<script setup lang="ts">
import { computed, markRaw, nextTick, onBeforeUnmount, onMounted, shallowRef, watch } from 'vue';
import * as echarts from 'echarts';
import type { EChartsOption } from 'echarts';

const props = withDefaults(
	defineProps<{
		option: EChartsOption;
		theme?: string;
		height?: string | number;
		width?: string | number;
		autoresize?: boolean;
		notMerge?: boolean;
		lazyUpdate?: boolean;
	}>(),
	{
		theme: undefined,
		height: '100%',
		width: '100%',
		autoresize: true,
		notMerge: true,
		lazyUpdate: false,
	}
);

const elRef = shallowRef<HTMLElement>();
const chartRef = shallowRef<echarts.ECharts>();
let resizeObserver: ResizeObserver | null = null;

const toCssSize = (v: string | number) => (typeof v === 'number' ? `${v}px` : v);
const rootStyle = computed(() => ({
	width: toCssSize(props.width),
	height: toCssSize(props.height),
}));

const init = () => {
	const el = elRef.value;
	if (!el) return;
	chartRef.value?.dispose();
	chartRef.value = markRaw(echarts.init(el, props.theme));
};

const render = () => {
	const chart = chartRef.value;
	if (!chart) return;
	chart.setOption(props.option, { notMerge: props.notMerge, lazyUpdate: props.lazyUpdate });
	chart.resize();
};

const scheduleInitAndRender = async () => {
	await nextTick();
	init();
	render();
};

const attachResizeObserver = () => {
	if (!props.autoresize) return;
	const el = elRef.value;
	if (!el) return;
	if (!resizeObserver) {
		resizeObserver = new ResizeObserver(() => {
			chartRef.value?.resize();
		});
	}
	resizeObserver.observe(el);
};

onMounted(async () => {
	await scheduleInitAndRender();
	attachResizeObserver();
});

watch(
	() => props.theme,
	async () => {
		await scheduleInitAndRender();
	},
	{ flush: 'post' }
);

watch(
	() => props.option,
	() => {
		nextTick(() => {
			render();
		});
	},
	{ deep: true, flush: 'post' }
);

onBeforeUnmount(() => {
	resizeObserver?.disconnect();
	resizeObserver = null;
	chartRef.value?.dispose();
	chartRef.value = undefined;
});

defineExpose({
	resize: () => chartRef.value?.resize(),
	getInstance: () => chartRef.value,
});
</script>

<style scoped lang="scss">
.echarts-root {
	min-height: 1px;
}
</style>
