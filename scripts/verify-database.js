/**
 * 验证数据库脚本 - 验证 spec 和 part 相关数据
 *
 * 运行: node scripts/verify-database.js
 *
 * 验证内容:
 * 1. 模板数量和 paramDefs
 * 2. 分类数量和 specParams 复制
 * 3. 配件数量和规格
 * 4. 多级分类规格合并（子级覆盖父级）
 * 5. 级联更新测试
 */

const { MongoClient } = require('mongodb');

const API_BASE = 'http://localhost:5128/api';
const MONGODB_CONFIG = {
  connectionString: 'mongodb://admin:ganwei.123@211.159.151.178:21117',
  databaseName: 'inventory_db'
};

let authToken = null;

async function login() {
  const res = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'admin', password: 'admin123' })
  });
  const data = await res.json();
  authToken = data.token;
  console.log('✓ 登录成功');
}

async function apiGet(endpoint) {
  const res = await fetch(`${API_BASE}${endpoint}`, {
    headers: { Authorization: `Bearer ${authToken}` }
  });
  return res.json();
}

async function apiPut(endpoint, data) {
  const res = await fetch(`${API_BASE}${endpoint}`, {
    method: 'PUT',
    headers: {
      Authorization: `Bearer ${authToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(data)
  });
  return res.json();
}

async function verifyByApi() {
  console.log('\n========== API 验证 ==========\n');

  // 1. 验证模板
  console.log('1. 验证模板');
  const templates = await apiGet('/templates');
  console.log(`   模板数量: ${templates.length}`);
  templates.forEach(t => {
    console.log(`   - ${t.category}: ${t.paramDefs.length} 个参数`);
    t.paramDefs.forEach(p => {
      console.log(`     · ${p.key} (${p.label}) ${p.unit ? '[' + p.unit + ']' : ''}`);
    });
  });

  // 2. 验证分类
  console.log('\n2. 验证分类');
  const categories = await apiGet('/part-categories');
  console.log(`   分类数量: ${categories.length}`);

  const categoryMap = {};
  categories.forEach(c => {
    categoryMap[c.id] = c;
    const templateTag = c.specTemplateId ? ' [关联模板]' : '';
    console.log(`   - ${c.path}${templateTag}`);
    if (c.specParams && c.specParams.length > 0) {
      console.log(`     specParams: ${c.specParams.length} 个`);
      c.specParams.forEach(p => {
        console.log(`     · ${p.key} ${p.unit ? '[' + p.unit + ']' : ''}`);
      });
    }
  });

  // 3. 验证配件
  console.log('\n3. 验证配件');
  const parts = await apiGet('/parts');
  console.log(`   配件数量: ${parts.length}`);
  parts.slice(0, 5).forEach(p => {
    console.log(`   - ${p.name} (${p.category})`);
    if (p.specs) {
      p.specs.forEach(s => {
        console.log(`     · ${s.key}: ${s.value} ${s.unit || ''}`);
      });
    }
  });
  if (parts.length > 5) {
    console.log(`   ... 还有 ${parts.length - 5} 个`);
  }

  // 4. 验证多级分类规格合并
  console.log('\n4. 验证多级分类规格合并（子级覆盖父级）');

  // 找电机/伺服电机/直流伺服 分类
  const dcServo = categories.find(c => c.path === '电机/伺服电机/直流伺服');
  const motor = categories.find(c => c.path === '电机');
  const servo = categories.find(c => c.path === '电机/伺服电机');

  if (dcServo) {
    console.log(`\n   直流伺服 specs (本地): ${dcServo.specParams?.length || 0} 个`);

    // 计算合并后的规格（手动递归）
    function getMergedSpecParams(cat) {
      let params = [];
      if (cat.parentId && categoryMap[cat.parentId]) {
        params = getMergedSpecParams(categoryMap[cat.parentId]);
      }
      if (cat.specParams && cat.specParams.length > 0) {
        const currentKeys = new Set(cat.specParams.map(p => p.key));
        params = [
          ...params.filter(p => !currentKeys.has(p.key)),
          ...cat.specParams
        ];
      }
      return params;
    }

    const mergedParams = getMergedSpecParams(dcServo);
    console.log(`   直流伺服 specs (合并后): ${mergedParams.length} 个`);
    mergedParams.forEach(p => {
      console.log(`     · ${p.key} (${p.label}) ${p.unit ? '[' + p.unit + ']' : ''}`);
    });

    // 验证功率单位是否被覆盖
    const powerParam = mergedParams.find(p => p.key === 'power');
    if (powerParam && powerParam.unit === 'W') {
      console.log(`\n   ✓ 功率单位正确覆盖: kW → W`);
    } else if (powerParam) {
      console.log(`\n   ✗ 功率单位覆盖错误: ${powerParam.unit}`);
    }
  }

  // 5. 测试级联更新
  console.log('\n5. 验证级联更新');

  const templateA = templates.find(t => t.category === '电机通用模板');
  if (templateA && motor) {
    console.log(`   原始电机分类功率单位: ${motor.specParams?.find(p => p.key === 'power')?.unit}`);

    // 更新模板
    await apiPut(`/templates/${templateA.id}`, {
      category: '电机通用模板',
      paramDefs: [
        { key: 'power', label: '功率', unit: 'MW', dataType: 'number', options: [], required: false },
        { key: 'voltage', label: '电压', unit: 'V', dataType: 'number', options: [], required: false },
        { key: 'efficiency', label: '效率等级', unit: '', dataType: 'select', options: ['IE1', 'IE2', 'IE3', 'IE4'], required: false }
      ]
    });
    console.log(`   模板A功率单位已改为 MW`);

    // 重新获取电机分类
    const updatedMotor = await apiGet(`/part-categories/${motor.id}`);
    const updatedPowerUnit = updatedMotor.specParams?.find(p => p.key === 'power')?.unit;
    console.log(`   更新后电机分类功率单位: ${updatedPowerUnit}`);

    if (updatedPowerUnit === 'MW') {
      console.log(`   ✓ 级联更新成功: 模板更新后分类自动更新`);
    } else {
      console.log(`   ✗ 级联更新失败`);
    }

    // 恢复模板
    await apiPut(`/templates/${templateA.id}`, {
      category: '电机通用模板',
      paramDefs: [
        { key: 'power', label: '功率', unit: 'kW', dataType: 'number', options: [], required: false },
        { key: 'voltage', label: '电压', unit: 'V', dataType: 'number', options: [], required: false },
        { key: 'efficiency', label: '效率等级', unit: '', dataType: 'select', options: ['IE1', 'IE2', 'IE3', 'IE4'], required: false }
      ]
    });
    console.log(`   模板A已恢复为 kW`);
  }

  console.log('\n========== API 验证完成 ==========');
}

async function verifyByMongoDB() {
  console.log('\n========== MongoDB 直接验证 ==========\n');

  const client = new MongoClient(MONGODB_CONFIG.connectionString);

  try {
    await client.connect();
    const db = client.db(MONGODB_CONFIG.databaseName);

    // 1. 模板 (使用 category 字段查询)
    const templates = await db.collection('templates').find({}).toArray();
    console.log('1. 模板集合');
    console.log(`   数量: ${templates.length}`);
    templates.forEach(t => {
      console.log(`   - ${t.category || t.name}: paramDefs[${t.paramDefs?.length || 0}]`);
    });

    // 2. 分类 (使用 name 字段)
    const categories = await db.collection('part_categories').find({}).toArray();
    console.log('\n2. 分类集合');
    console.log(`   数量: ${categories.length}`);
    categories.forEach(c => {
      const templateInfo = c.specTemplateId ? ` [templateId: ${c.specTemplateId}]` : '';
      console.log(`   - ${c.name || c.path}${templateInfo}`);
      if (c.specParams) {
        console.log(`     specParams: ${c.specParams.length} 个参数`);
      }
    });

    // 3. 配件
    const parts = await db.collection('parts').find({}).toArray();
    console.log('\n3. 配件集合');
    console.log(`   数量: ${parts.length}`);
    parts.slice(0, 3).forEach(p => {
      console.log(`   - ${p.name}`);
      console.log(`     category: ${p.category}`);
      if (p.specs) {
        console.log(`     specs: ${p.specs.length} 个`);
      }
    });

    // 4. 验证 specParams 冗余存储
    console.log('\n4. 验证 specParams 冗余存储');
    const categoriesWithSpecParams = categories.filter(c => c.specParams && c.specParams.length > 0);
    console.log(`   有 specParams 的分类: ${categoriesWithSpecParams.length}`);

    if (categoriesWithSpecParams.length > 0) {
      const sample = categoriesWithSpecParams[0];
      console.log(`   示例: ${sample.name || sample.path}`);
      console.log(`   specParams: ${JSON.stringify(sample.specParams.slice(0, 2))}`);
    }

    console.log('\n========== MongoDB 验证完成 ==========');

  } catch (error) {
    console.error('✗ MongoDB 验证失败:', error.message);
  } finally {
    await client.close();
  }
}

async function main() {
  try {
    await login();
    await verifyByApi();
    await verifyByMongoDB();
  } catch (error) {
    console.error('验证失败:', error);
    process.exit(1);
  }
}

main();
