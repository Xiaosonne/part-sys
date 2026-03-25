/**
 * 清空数据库脚本 - 清除 spec 和 part 相关数据
 *
 * 运行: node scripts/clear-database.js
 *
 * 清空集合:
 * - parts
 * - part_categories
 * - templates
 * - stock_transactions (可选)
 */

const { MongoClient } = require('mongodb');

const MONGODB_CONFIG = {
  connectionString: 'mongodb://admin:ganwei.123@211.159.151.178:21117',
  databaseName: 'inventory_db'
};

async function clearDatabase() {
  const client = new MongoClient(MONGODB_CONFIG.connectionString);

  try {
    await client.connect();
    console.log('✓ MongoDB 连接成功');

    const db = client.db(MONGODB_CONFIG.databaseName);

    // 清空集合函数
    async function clearCollection(collectionName) {
      const collection = db.collection(collectionName);
      const count = await collection.countDocuments();
      await collection.deleteMany({});
      console.log(`✓ ${collectionName}: 已清空 ${count} 条数据`);
    }

    // 清空相关集合
    console.log('\n========== 开始清空数据库 ==========\n');

    await clearCollection('parts');
    await clearCollection('part_categories');
    await clearCollection('templates');
    await clearCollection('stock_transactions');

    console.log('\n========== 数据库已清空 ==========');

  } catch (error) {
    console.error('✗ MongoDB 操作失败:', error.message);
    process.exit(1);
  } finally {
    await client.close();
    console.log('✓ MongoDB 连接已关闭');
  }
}

clearDatabase();
