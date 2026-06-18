export type TimeQuerySeriesItem = {
	seriesName?: string;
	seriesUnit?: string;
	seriesValueDatas?: Array<{ xaxis?: string; xAxis?: string; value?: string | number }>;
	serieValueDatas?: Array<{ xaxis?: string; xAxis?: string; value?: string | number }>;
};

export type SeriesData = { xAxis: string[]; values: number[] };

export type PeriodValue = 'year' | 'month';
export type TimeValue = '' | string | [string, string];

export function resolveTimeRange(timeType: PeriodValue | string, time: TimeValue | unknown) {
	if (timeType === 'month') {
		if (Array.isArray(time)) return { startTime: String(time[0] ?? ''), endTime: String(time[1] ?? '') };
		if (typeof time === 'string') return { startTime: time, endTime: time };
		return { startTime: '', endTime: '' };
	}
	const year = String((time as any) || new Date().getFullYear());
	return { startTime: year, endTime: year };
}

export function pickTimeQuerySeriesFirstValue(list: unknown, seriesName: string): { value: number; unit: string } {
	const seriesList: TimeQuerySeriesItem[] = Array.isArray(list) ? (list as TimeQuerySeriesItem[]) : [];
	const s =
		seriesList.find((item) => String(item?.seriesName ?? '') === seriesName) ??
		seriesList.find((item) => String(item?.seriesName ?? '').includes(seriesName));
	const rawPoints = s?.serieValueDatas ?? s?.seriesValueDatas;
	const first = Array.isArray(rawPoints) ? rawPoints[0] : undefined;
	const value = Number((first as any)?.value ?? 0);
	return { value: Number.isFinite(value) ? value : 0, unit: String(s?.seriesUnit ?? '') };
}

export function fillTimeChartsFromTimeQueryList<
	TId extends string,
	TCfg extends { id: TId; xAxisFrom: string[]; series: Array<{ name: string }> },
>(list: unknown, configById: Record<TId, TCfg>, chartXAxis: Record<TId, string[]>, seriesData: Record<string, SeriesData>) {
	const seriesList: TimeQuerySeriesItem[] = Array.isArray(list) ? (list as TimeQuerySeriesItem[]) : [];
	const pickSeries = (seriesName: string): SeriesData => {
		const s =
			seriesList.find((item) => String(item?.seriesName ?? '') === seriesName) ??
			seriesList.find((item) => String(item?.seriesName ?? '').includes(seriesName));
		const rawPoints = s?.seriesValueDatas ?? s?.serieValueDatas;
		const points = Array.isArray(rawPoints) ? (rawPoints as Array<{ xaxis?: string; xAxis?: string; value?: string | number }>) : [];
		const xAxis = points.map((p) => String(p?.xaxis ?? p?.xAxis ?? ''));
		const values = points.map((p) => Number(p?.value ?? 0));
		return { xAxis, values };
	};
	const seriesCache = new Map<string, SeriesData>();
	const pick = (name: string) => {
		const cached = seriesCache.get(name);
		if (cached) return cached;
		const picked = pickSeries(name);
		seriesCache.set(name, picked);
		return picked;
	};

	for (const cfg of Object.values(configById)) {
		let xAxis: string[] = [];
		for (const name of cfg.xAxisFrom) {
			const s = pick(name);
			if (s.xAxis.length) {
				xAxis = s.xAxis;
				break;
			}
		}
		chartXAxis[cfg.id] = xAxis;
		for (const s of cfg.series) {
			seriesData[s.name] = pick(s.name);
		}
	}
}
