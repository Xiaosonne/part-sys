/**
 * 迁移脚本 001: 为 PartCategory 补充 specParams 字段
 *
 * 功能：从关联的 SpecTemplate 复制 paramDefs 到 PartCategory.specParams
 * 运行：node scripts/migration/001_backfill_spec_params.js
 */

const { MongoClient } = require('mongodb');

const MONGO_URI = 'mongodb://localhost:27017';
const DB_NAME = 'InventorySystem';

async function migrate() {
  const client = new MongoClient(MONGO_URI);

  try {
    await client.connect();
    console.log('Connected to MongoDB');

    const db = client.db(DB_NAME);
    const categories = db.collection('partcategories');
    const templates = db.collection('spectemplates');

    // 构建模板ID到参数的映射
    const templateMap = {};
    const allTemplates = await templates.find({}).toArray();
    for (const t of allTemplates) {
      templateMap[t._id.toString()] = t.paramDefs || [];
    }
    console.log(`Loaded ${allTemplates.length} templates`);

    // 更新所有有 specTemplateId 但 specParams 为空的分类
    const categoriesToUpdate = await categories.find({
      specTemplateId: { $ne: null },
      $or: [
        { specParams: { $exists: false } },
        { specParams: null }
      ]
    }).toArray();

    console.log(`Found ${categoriesToUpdate.length} categories needing update`);

    let updatedCount = 0;
    for (const cat of categoriesToUpdate) {
      const params = templateMap[cat.specTemplateId];
      if (params && params.length > 0) {
        await categories.updateOne(
          { _id: cat._id },
          { $set: { specParams: params } }
        );
        updatedCount++;
        console.log(`  Updated: ${cat.name} (${cat.path}) - ${params.length} params`);
      } else {
        console.log(`  Warning: Template ${cat.specTemplateId} not found for ${cat.name}`);
      }
    }

    console.log(`\nMigration complete: ${updatedCount} categories updated`);

    // 验证
    console.log('\n--- Verification ---');
    const categoriesWithTemplate = await categories.countDocuments({
      specTemplateId: { $ne: null }
    });
    const categoriesWithSpecParams = await categories.countDocuments({
      specTemplateId: { $ne: null },
      specParams: { $exists: true, $ne: null, $ne: [] }
    });
    const categoriesWithoutSpecParams = await categories.countDocuments({
      specTemplateId: { $ne: null },
      $or: [
        { specParams: { $exists: false } },
        { specParams: null },
        { specParams: [] }
      ]
    });

    console.log(`Categories with template: ${categoriesWithTemplate}`);
    console.log(`Categories with specParams: ${categoriesWithSpecParams}`);
    console.log(`Categories without specParams (warning): ${categoriesWithoutSpecParams}`);

    if (categoriesWithoutSpecParams > 0) {
      console.log('\nWARNING: Some categories have template but no specParams!');
    }

  } catch (error) {
    console.error('Migration failed:', error);
    process.exit(1);
  } finally {
    await client.close();
  }
}

migrate();
